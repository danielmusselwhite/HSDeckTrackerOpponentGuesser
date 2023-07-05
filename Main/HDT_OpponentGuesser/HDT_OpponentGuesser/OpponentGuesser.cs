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

namespace HDT_OpponentGuesser
{
    public class OpponentGuesser
    {
        private GameV2 _game;
        private Player _opponent;
        private string _class;
        private JToken _metaClassDecks;
        private bool _firstTurn;
        // Dictionary of card dbfId to a dict of strings containing the rest of the cards info
        private Dictionary<int, Dictionary<string, string>> _dbfIdToCardInfo;

        private BestFitDeckDisplay _bfdDisplay; // reference to the BestFitDeckDisplay class to display this information on the screen to the user
        private string _allMetaDecks; // string containing all meta decks from the API call

        private double _minimumMatch = 50; // minimum % of cards that must match for a deck to be considered a possible match

        // Creating constructor that takes in a reference to the BestFitDeckDisplay class
        public OpponentGuesser(BestFitDeckDisplay displayBestFitDeck)
        {
            // Getting reference to the game
            _game = Hearthstone_Deck_Tracker.Core.Game;

            // Getting reference to the card Dictionary
            _dbfIdToCardInfo = CardInfoDictionary.GetCardDictionary();
            
            // Getting reference to the BestFitDeckDisplay (the actual GUI) class
            _bfdDisplay = displayBestFitDeck;
            _bfdDisplay.SetMinimumMatch(_minimumMatch);
            if (Config.Instance.HideInMenu && _game.IsInMenu)
                _bfdDisplay.Hide();
            

            #region Do an API call to get a list of all meta decks
            // Create the URL
            string url = "https://hsreplay.net/analytics/query/list_decks_by_win_rate_v2/?GameType=RANKED_STANDARD&LeagueRankRange=GOLD&Region=ALL&TimeRange=CURRENT_PATCH";


            // Create the HTTP client
            HttpClient client = new HttpClient();

            // Make the API call
            HttpResponseMessage response = client.GetAsync(url).Result;

            // Get the response content
            HttpContent content = response.Content;

            // Get the string content
            _allMetaDecks = content.ReadAsStringAsync().Result;
            content.Dispose();
            client.Dispose();
            #endregion
        }


        // Triggered when a new game starts
        internal void GameStart()
        {
            // set the private variables
            _opponent = null; // reset the opponent for this new game
            _class = null; // reset the class for this new game
            _firstTurn = true; // reset the first turn for this new game
            _metaClassDecks = null;
            _bfdDisplay.Update(null); // update the display to show the default text
            _bfdDisplay.Show(); // show the display
        }

        // Triggered when a turn starts
        internal void TurnStart(ActivePlayer player)
        {
            // Couldn't be done in GameStart, as the opponent's class is not known until the first turn

            // setting up params on the first turn of the game once opponent has loaded in
            if (_firstTurn)
            {
                _opponent = _game.Opponent;
                _class = _opponent.Class;
                _class = _class.ToUpper();

                #region Parsing the _allMetaDecks JSON String to get the list of meta decks for this class
                // Convert the string content to a JSON object
                string stringContent = _allMetaDecks;
                dynamic jsonContent = JsonConvert.DeserializeObject(stringContent);

                // Get the jsonContent.series.data and store it to "allDecks"
                JObject allDecks = jsonContent.series.data;

                // Get the Decks for this class only
                _metaClassDecks = allDecks[_class];


                #region Transform deck_list into a 1D array
                // _metaClassDecks[i][deck_list] is in the format "[[cardID, numberInDeck],[cardID, numberInDeck] ...]"
                // we want it in format [cardID, cardID, cardID, ...]

                // for each deck in _metaClassDecks
                for (int i = 0; i < _metaClassDecks.Count(); i++)
                {
                    // first convert the string to a matrix
                    List<List<int>> deckCardsMatrix = JsonConvert.DeserializeObject<List<List<int>>>(_metaClassDecks[i]["deck_list"].ToString());

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
                    _metaClassDecks[i]["deck_list"] = "[" + string.Join(",", deckCards) + "]";


                }

                #endregion

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




            #region Determining the best fit meta deck (by comparing % of played cards that are in each deck)
            // creating list of deckPlayedCards dbfId fields
            List<int> deckPlayedCardsDbfId = new List<int>();
            foreach (Card cardPlayed in deckPlayedCards)
            {
                deckPlayedCardsDbfId.Add(cardPlayed.DbfId);
            }
            // Log the contents of deckPlayedCardsDbfId
            string deckPlayedCardsDbfIdString = string.Join(", ", deckPlayedCardsDbfId);


            // Loop through _metaClassDecks and find which has the most cards in common with deckPlayedCards
            int bestFitDeckIndex = -1;
            double bestFitDeckMatchPercent = _minimumMatch; // we are checking for strict improvement, so this value means we only consider guessing decks that match more than this percentage
            double bestWinRate = -1;
            Log.Info("Looping through _metaClassDecks ...");
            for (int i = 0; i < _metaClassDecks.Count(); i++)
            {
                List<int> deckList = JsonConvert.DeserializeObject<List<int>>(_metaClassDecks[i]["deck_list"].ToString());
                int matchCount = 0;

                // Loop through the deckPlayedCardsDbfId
                foreach (int cardDbfId in deckPlayedCardsDbfId)
                {
                    // If this card is in the deckList, increment the matchCount and pop only the first instance of it from the deckList so it can't be matched again
                    if (deckList.Contains(cardDbfId))
                    {
                        matchCount++;
                        deckList.Remove(cardDbfId);
                    }
                }

                // Calculate the match percentage and winrate
                double matchPercent = (double)matchCount / (double)deckPlayedCardsDbfId.Count() * 100;
                double winRate = (double)_metaClassDecks[i]["win_rate"];

                // If this deck has a higher match percentage than the previous best fit, replace it
                if (matchPercent > bestFitDeckMatchPercent)
                {
                    bestFitDeckIndex = i;
                    bestFitDeckMatchPercent = matchPercent;
                    bestWinRate = winRate;
                }
                // If this deck has an equal match percentage, then pick the one with the highest winrate
                else if (matchPercent == bestFitDeckMatchPercent)
                {
                    if (winRate > bestWinRate)
                    {
                        bestFitDeckIndex = i;
                        bestFitDeckMatchPercent = matchPercent;
                        bestWinRate = winRate;
                    }
                }
            }
            #endregion

            #region Updating UI for the best fit meta deck
            // If we found a best fit deck...
            if (bestFitDeckIndex != -1)
            {
                // Get the archetype_id and deck_id of the best fit deck
                JToken bestFitDeck = _metaClassDecks[bestFitDeckIndex];
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
                #endregion
                #endregion

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
                _bfdDisplay.Update(bestDeckName, bestWinRate, bestFitDeckMatchPercent, bestFitDeckId, guessedDeckListCardInfo, deckPlayedCardsCardInfo);
            }
            else
            {
                Log.Info("No deck has a match greater than the minimum of " + _minimumMatch + "%");

                // Display that there is no matching deck in the overlay
                _bfdDisplay.Update(null);
            }
            #endregion
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

                Log.Info("Adding card " + cardName);
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
