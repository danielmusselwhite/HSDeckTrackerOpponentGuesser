using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

// Added additional ones I require - these will need their DLL's manually added to HSReplay
using Newtonsoft.Json;

// Adding the hearthstone deck tracker references required for creating the plugin
using System.Windows.Controls;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker.Plugins;
using Hearthstone_Deck_Tracker.Utility.Logging;
using Newtonsoft.Json.Linq;
using Hearthstone_Deck_Tracker;
using System.Windows.Forms.VisualStyles;

namespace HDT_OpponentGuesser
{
    public class OpponentGuesser
    {
        private GameV2 _game;
        private Player _opponent;
        private string _oppClass;
        private JToken _metaOppClassDecks;
        private JToken _metaUserClassDecks;
        private bool _firstTurn;
        // Dictionary of card dbfId to a dict of strings containing the rest of the cards info
        private Dictionary<int, Dictionary<string, string>> _dbfIdToCardInfo;
        private Dictionary<int, Dictionary<int, double>> _matchups;

        private BestFitDeckDisplay _bfdDisplay; // reference to the BestFitDeckDisplay class to display this information on the screen to the user
        private string _allMetaDecks; // string containing all meta decks from the API call

        private double _minimumMatch = 50; // minimum % of cards that must match for a deck to be considered a possible match

        private Nullable<int> _playerArchetype; // the archetype of the player's deck (used in getting matchups)

        // Creating constructor that takes in a reference to the BestFitDeckDisplay class
        public OpponentGuesser(BestFitDeckDisplay displayBestFitDeck)
        {
            // Getting reference to the game
            _game = Hearthstone_Deck_Tracker.Core.Game;

            // Getting reference to the card Dictionary
            _dbfIdToCardInfo = CardInfoDictionary.GetCardDictionary();

            // Getting reference to the matchup Dictionary
            _matchups = MatchUpsDictionary.GetMatchUpsDictionary();
            
            // Getting reference to the BestFitDeckDisplay (the actual GUI) class
            _bfdDisplay = displayBestFitDeck;
            _bfdDisplay.SetMinimumMatch(_minimumMatch);
            if (Config.Instance.HideInMenu && _game.IsInMenu)
                _bfdDisplay.Hide();


            #region 
            _allMetaDecks = GetAllMetaDecks();
            #endregion
        }

        // Function to do an API call to get a list of all meta decks
        private string GetAllMetaDecks()
        {
            HttpClient client;
            HttpResponseMessage response;
            try
            {
                client = new HttpClient();
                response = client.GetAsync("https://hsreplay.net/analytics/query/list_decks_by_win_rate_v2/?GameType=RANKED_STANDARD&LeagueRankRange=GOLD&Region=ALL&TimeRange=CURRENT_PATCH").Result;
                response.EnsureSuccessStatusCode();
                if (response.Content == null)
                {
                    throw new Exception("response.Content was null");
                }
            }
            // if it fails, do same query but remove current_patch filter
            catch
            {
                Log.Info("Getting matchups for current_patch failed (likely a new patch and backend hasn't been updated yet; trying for all patches");
                client = new HttpClient();
                response = client.GetAsync("https://hsreplay.net/analytics/query/list_decks_by_win_rate_v2/?GameType=RANKED_STANDARD&LeagueRankRange=GOLD&Region=ALL").Result;
            }

            // Get the string content
            HttpContent content = response.Content;
            return content.ReadAsStringAsync().Result;
        } 


        // Triggered when a new game starts
        internal void GameStart()
        {
            // set the private variables
            _opponent = null; // reset the opponent for this new game
            _oppClass = null; // reset the class for this new game
            _firstTurn = true; // reset the first turn for this new game
            _metaOppClassDecks = null;
            _metaUserClassDecks = null;
            _bfdDisplay.Update(null); // update the display to show the default text
            _bfdDisplay.Show(); // show the display

            _matchups = MatchUpsDictionary.GetMatchUpsDictionary(); // call for matchupsdictionary singleton (if it doesn't exist due to API being down, it will be created else will remain)
        }

        // Triggered when a turn starts
        internal void TurnStart(ActivePlayer player)
        {
            // Couldn't be done in GameStart, as the opponent's class is not known until the first turn

            // setting up params on the first turn of the game once opponent has loaded in
            if (_firstTurn)
            {
                Log.Info("This is the first turn!");
                _opponent = _game.Opponent;
                _oppClass = _opponent.Class;
                _oppClass = _oppClass.ToUpper();

                Log.Info("_oppClass = " + _oppClass);
                Log.Info("_userClass = " + _game.Player.Class.ToUpper());

                #region Parsing the _allMetaDecks JSON String to get the list of meta decks for this class
                // Convert the string content to a JSON object
                string stringContent = _allMetaDecks;
                dynamic jsonContent = JsonConvert.DeserializeObject(stringContent);

                // Get the jsonContent.series.data and store it to "allDecks"
                JObject allDecks = jsonContent.series.data;

                // Get the Decks for this class only
                _metaOppClassDecks = allDecks[_oppClass];
                _metaOppClassDecks = TransformDeckListTo1D(_metaOppClassDecks);
                _metaUserClassDecks = allDecks[_game.Player.Class.ToUpper()];
                _metaUserClassDecks = TransformDeckListTo1D(_metaUserClassDecks);

                Log.Info("test");
                Log.Info(""+_metaOppClassDecks.ToString());

                #region Getting the best fit deck for the Player so we can get matchups
                // get a list of dbfIds of the cards in the users deck
                List<int> playerCardsDbf = new List<int>();
                foreach (Card playerCard in _game.Player.PlayerCardList)
                {
                    // for the count of the card, add the dbfId to the list
                    for (int i = 0; i < playerCard.Count; i++)
                        playerCardsDbf.Add(playerCard.DbfId);
                }
                #endregion

                // get the players best matching archetype
                (int, double, bool) results = GetBestFitDeck(playerCardsDbf, _metaUserClassDecks);
                int playerDeckIndex = results.Item1;
                _playerArchetype = results.Item3 ? (Nullable<int>)_metaUserClassDecks[playerDeckIndex]["archetype_id"] : null;
                Log.Info("user is playing playerArchetype: " + _playerArchetype);

                _firstTurn = false;
                #endregion
            }
        }



        // Triggered when the opponent plays a card
        internal void OpponentPlay(Card card)
        {
            #region Getting the list of cards the opponent played that originated from their deck
            // Log some core info on the card that was just played
            Log.Info($"Opponent played {card.Name} ({card.Id}), {card.Cost}, {card.DbfId}");

            // Get the List of all cards played
            List<Card> allPlayedCards = _opponent.OpponentCardList;
            allPlayedCards.Add(card);
            string allPlayedCardsString = string.Join(", ", allPlayedCards.Select(c => c.Name));
            Log.Info($"Opponent has played {allPlayedCards.Count()} cards in total ({allPlayedCardsString})");

            // Filter by cards that originated from their deck
            List<Card> deckPlayedCards = allPlayedCards.Where(c => !c.IsCreated && c.Collectible).ToList(); // cards must be collectible (eg not a token) and have not been created
            string deckPlayedCardsString = string.Join(", ", deckPlayedCards.Select(c => c.Name));
            Log.Info($"Opponent has played {deckPlayedCards.Count()} cards from their deck in total ({deckPlayedCardsString})");
            #endregion




            
            // creating list of deckPlayedCards dbfId fields
            List<int> deckPlayedCardsDbfId = new List<int>();
            foreach (Card cardPlayed in deckPlayedCards)
            {
                // for the count of the card, add the dbfId to the list
                for (int i = 0; i < cardPlayed.Count; i++)
                    deckPlayedCardsDbfId.Add(cardPlayed.DbfId);
            }
            // Log the contents of deckPlayedCardsDbfId
            string deckPlayedCardsDbfIdString = string.Join(", ", deckPlayedCardsDbfId);

            // get bestFitDeck by calling method for _metaOppClassDecks
            (int, double, bool) results = GetBestFitDeck(deckPlayedCardsDbfId, _metaOppClassDecks);
            int bestFitDeckIndex = results.Item1;
            double bestFitDeckMatchPercent = results.Item2;
            bool bestFitDeckIsUserDeck = results.Item3;

            #region Updating UI for the best fit meta deck
            // If we found a best fit deck...
            if (bestFitDeckIsUserDeck)
            {
                // Get the archetype_id and deck_id of the best fit deck
                JToken bestFitDeck = _metaOppClassDecks[bestFitDeckIndex];
                string archetypeId = bestFitDeck["archetype_id"].ToString();
                string bestFitDeckId = bestFitDeck["deck_id"].ToString();


                // API call to get name of deck with this archetype_id
                #region Getting the name of the Deck
                #region Do an API call to get info on this decks archetype
                // Create the URL
                string url = $"https://hsreplay.net/api/v1/archetypes/{archetypeId}";

                // Create the HTTP client
                HttpClient client = new HttpClient();

                // Make the API call
                HttpResponseMessage response = client.GetAsync(url).Result;

                // Get the response content
                HttpContent content = response.Content;

                // Get the string content
                string stringContent = content.ReadAsStringAsync().Result;
                content.Dispose();
                client.Dispose();
                #endregion

                #region Getting the archetype name from this info
                // Convert the string content to a JSON object
                dynamic jsonContent = JsonConvert.DeserializeObject(stringContent);

                // Get the name from this JSON object
                string bestDeckName = jsonContent["name"];

                // if there is no name, then use the class name instead
                if (bestDeckName == null)
                {
                    bestDeckName = _opponent.Class.ToString()+" deck";
                }
                #endregion
                #endregion


                // getting the matchup winrate for this deck
                Log.Info("matchups dict: " + _matchups);
                Log.Info("getting matchup winrate for this deck");
                Log.Info("playerArchetype: " + _playerArchetype);

                double bestWinRate = -1;
                bool matchup = true;
                // try to find the matchup winrate for players archetype vs opponents archetype; if it doesn't exist, then use the overall winrate for opponents archetype
                try
                {
                    Log.Info("trying to get matchup winrate of opponents deck vs you");
                    bestWinRate = _matchups[Int32.Parse(archetypeId)][(int)_playerArchetype];
                }
                catch
                {
                    Log.Info("matchup winrate for this deck doesn't exist, so using overall winrate for opponents archetype");
                    bestWinRate = (double)bestFitDeck["win_rate"];
                    matchup = false;
                }
                Log.Info("bestWinRate: " + bestWinRate);

                Log.Info("Best fit deck is archetype " + bestDeckName + " at index " + bestFitDeckIndex + " (" + bestFitDeckId + ") with a " + bestFitDeckMatchPercent + "% match (greater than minimum of " + _minimumMatch + "%) and a " + bestWinRate + "% winrate");

                // iterate through each card in bestFitDeck and create a Card entity for it storing all them in a list
                Log.Info("Creating a List<CardInfo> for the guessed deck");
                List<int> bestDeckDbfList = JsonConvert.DeserializeObject<List<int>>(bestFitDeck["deck_list"].ToString());
                List<CardInfo> guessedDeckListCardInfo = CreateCardInfoDeckFromDBF(bestDeckDbfList);
                
                //making deckPlayedCards into a List<CardInfo>
                Log.Info("Creating a List<CardInfo> for the playedCards");
                List<CardInfo> deckPlayedCardsCardInfo = CreateCardInfoDeckFromDBF(deckPlayedCardsDbfId);

                // Sorting the decks by the cost of the cards (descending)
                guessedDeckListCardInfo.Sort((x, y) => x.GetCost().CompareTo(y.GetCost()));
                guessedDeckListCardInfo.Reverse();
                deckPlayedCardsCardInfo.Sort((x, y) => x.GetCost().CompareTo(y.GetCost()));
                deckPlayedCardsCardInfo.Reverse();

                Log.Info("calling _bfdDisplay.Update()");
                // Display the deck name in the overlay
                _bfdDisplay.Update(bestDeckName, bestWinRate, bestFitDeckMatchPercent, bestFitDeckId, guessedDeckListCardInfo, deckPlayedCardsCardInfo, matchup);
            }
            else
            {
                Log.Info("No deck has a match greater than the minimum of " + _minimumMatch + "%");

                // Display that there is no matching deck in the overlay
                _bfdDisplay.Update(null);
            }
            #endregion
        }

        private (int, double, bool) GetBestFitDeck(List<int> deckPlayedCardsDbfId, JToken metaClassDecks) { 
            // Loop through metaClassDecks and find which has the most cards in common with deckPlayedCards
            int bestFitDeckIndex = -1;
            // we are checking for strict improvement, so this value means we only consider guessing decks that match more than this percentage
            // so as we have a minimumMatch we want to check greater than or equal to, default it to minimumMatch - 1
            Log.Info("Getting best fit deck list for : "+string.Join(",", deckPlayedCardsDbfId));
            Log.Info("Number of metaclassdecks = "+metaClassDecks.Count());

            double bestFitDeckMatchPercent = _minimumMatch - 1;
            for (int i = 0; i < metaClassDecks.Count(); i++)
            {
                Log.Info("Trying to desiarilize");
                Log.Info("Decklist: "+ metaClassDecks[i]["deck_list"]);
                List<int> deckList = JsonConvert.DeserializeObject<List<int>>(metaClassDecks[i]["deck_list"].ToString());
                int matchCount = 0;


                // Loop through the deckPlayedCardsDbfId
                foreach (int cardDbfId in deckPlayedCardsDbfId)
                {
                    // If this card is in the deckList, increment the matchCount and pop only the first instance of it from the deckList so it can't be matched again
                    if (deckList.Contains(cardDbfId))
                    {
                        Log.Info("Decklist i " + i + " contains card " + cardDbfId);
                        matchCount++;
                        deckList.Remove(cardDbfId);
                    }
                }


                // Calculate the match percentage and winrate
                double matchPercent = (double)matchCount / (double)deckPlayedCardsDbfId.Count() * 100;

                // If this deck has a higher match percentage than the previous best fit, replace it
                if (matchPercent > bestFitDeckMatchPercent)
                {
                    bestFitDeckIndex = i;
                    bestFitDeckMatchPercent = matchPercent;
                }
            }

            // return bestFitDeckIndex, bestFitDeckMatchPercent
            Log.Info("Returning: index=" + bestFitDeckIndex + " match=" + bestFitDeckIndex + "%");
            return (bestFitDeckIndex, bestFitDeckMatchPercent, bestFitDeckIndex!=-1);
        }

        private JToken TransformDeckListTo1D(JToken metaClassDecks)
        {
            // metaClassDecks[i][deck_list] is in the format "[[cardID, numberInDeck],[cardID, numberInDeck] ...]"
            // we want it in format [cardID, cardID, cardID, ...]

            // for each deck in metaClassDecks
            for (int i = 0; i < metaClassDecks.Count(); i++)
            {
                // first convert the string to a matrix
                List<List<int>> deckCardsMatrix = JsonConvert.DeserializeObject<List<List<int>>>(metaClassDecks[i]["deck_list"].ToString());

                // then convert the matrix to a 1D list, by adding deckCardsMatrix[i][0] to the list deckCards deckCardsMatrix[i][1] times
                List<int> deckCards = new List<int>();
                for (int j = 0; j < deckCardsMatrix.Count(); j++)
                {
                    for (int k = 0; k < deckCardsMatrix[j][1]; k++)
                    {
                        deckCards.Add(deckCardsMatrix[j][0]);
                    }
                }

                // then replace the deck_list's value with the 1D list
                metaClassDecks[i]["deck_list"] = "[" + string.Join(",", deckCards) + "]";


            }

            return metaClassDecks;
        }

        private List<CardInfo> CreateCardInfoDeckFromDBF(List<int> dbfIds)
        {
            List<CardInfo> deck = new List<CardInfo>();
            foreach (int cardDbfId in dbfIds)
            {
                // getting the cards info
                string cardName = (string)_dbfIdToCardInfo[cardDbfId]["name"];
                int cardCost = Int32.Parse((string)_dbfIdToCardInfo[cardDbfId]["cost"]);
                string cardDescription = (string)_dbfIdToCardInfo[cardDbfId]["description"];
                string cardType = (string)_dbfIdToCardInfo[cardDbfId]["type"];
                string cardAttack = (string)_dbfIdToCardInfo[cardDbfId]["attack"];
                string cardHealth = (string)_dbfIdToCardInfo[cardDbfId]["attack"];
                string rarity = (string)_dbfIdToCardInfo[cardDbfId]["rarity"];
                // Add the card to the list
                deck.Add(new CardInfo(cardDbfId, cardName, cardCost, cardHealth, cardAttack, cardDescription, cardType, rarity, false)); // false as not been played yet
            }

            return deck;
        }

        // Triggered when the player enters menu
        internal void InMenu()
        {
            // Hide the overlay
            if (Config.Instance.HideInMenu)
            {
                _bfdDisplay.Hide();
            }
        }

    }

}
