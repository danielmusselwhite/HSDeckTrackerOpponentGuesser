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
using System.Text.RegularExpressions;

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

        private double _minimumMatch; // minimum % of cards that must match for a deck to be considered a possible match

        private Nullable<int> _playerArchetype; // the archetype of the player's deck (used in getting matchups)

        // Creating constructor that takes in a reference to the BestFitDeckDisplay class
        public OpponentGuesser(BestFitDeckDisplay displayBestFitDeck)
        {
            // setting the minimum match percentage
            _minimumMatch = 50;

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

            // getting list of all the metadecks from hsreplay.net
            _allMetaDecks = MetaDecks.GetAllMetaDecks();
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

                // Get the Decks for this class only
                _metaOppClassDecks = MetaDecks.GetClassMetaDecks(_oppClass);
                _metaUserClassDecks = MetaDecks.GetClassMetaDecks(_game.Player.Class.ToUpper());

                // Get the players best fit deck so we can get matchups
                _playerArchetype = GetPlayerBestFit();
                Log.Info("user is playing playerArchetype: " + _playerArchetype);

                _firstTurn = false;
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
            List<int> deckPlayedCardsDbfId = GetDbfIDs(deckPlayedCards);
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
                string bestDeckName = MetaDecks.GetDeckArchetypeName(archetypeId, _oppClass);

                // getting the matchup winrate for this deck
                double bestWinRate = GetMatchupWinrate((int)_playerArchetype, Int32.Parse(archetypeId));
                bool matchup = true;
                if (bestWinRate == -1)
                {
                    bestWinRate = (double)bestFitDeck["win_rate"];
                    matchup = false;
                }

                Log.Info((matchup ? "bestWinRate MATCHUP: " : "bestWinRate OVERALL: ") + bestWinRate);

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
            double bestFitDeckMatchupWinrate = -1;
            double bestFitDeckWinrate = -1;
            // we are checking for strict improvement, so this value means we only consider guessing decks that match more than this percentage
            // so as we have a minimumMatch we want to check greater than or equal to, default it to minimumMatch - 1
            Log.Info("Getting best fit deck list for : "+string.Join(",", deckPlayedCardsDbfId));
            Log.Info("Number of metaclassdecks = "+metaClassDecks.Count());

            double bestFitDeckMatchPercent = _minimumMatch - 1;
            for (int i = 0; i < metaClassDecks.Count(); i++)
            {
                List<int> deckList = JsonConvert.DeserializeObject<List<int>>(metaClassDecks[i]["deck_list"].ToString());
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

                // Getting the matchup winrate + overall winrate of this deck

                // if _playerArchetype exists and metaClassDecks[i] has "archetype_id" ...
                double thisDeckMatchupWinrate = -1;
                if (_playerArchetype != null && metaClassDecks[i]["archetype_id"] != null)
                    thisDeckMatchupWinrate = GetMatchupWinrate((int)_playerArchetype, Int32.Parse((string)metaClassDecks[i]["archetype_id"]));
                double thisDeckWinrate = (double)metaClassDecks[i]["win_rate"];
                
                // If this deck has a higher match percentage than the previous best fit, replace it
                if (matchPercent > bestFitDeckMatchPercent)
                {
                    Log.Info("Match Percent Beaten: " + matchPercent + " > " + bestFitDeckMatchPercent);
                    Log.Info("New Best Fit Deck is" + i + " has matchup winrate " + thisDeckMatchupWinrate + " and overall winrate " + thisDeckWinrate + " and match percent " + matchPercent + "%");

                    bestFitDeckIndex = i;
                    bestFitDeckMatchPercent = matchPercent;
                    bestFitDeckMatchupWinrate = thisDeckMatchupWinrate;
                    bestFitDeckWinrate = thisDeckWinrate;
                }
                // If this deck has an equal match percentage pick the one with the higher winrate by indexing into _matchups
                else if (matchPercent == bestFitDeckMatchPercent)
                {
                    // if this deck has a greater matchup winrate then use this
                    if (thisDeckMatchupWinrate > bestFitDeckMatchupWinrate)
                    {
                        Log.Info("Match Percent Equal: " + matchPercent + " = " + bestFitDeckMatchPercent);
                        Log.Info("Matchup Winrate Beaten: " + thisDeckMatchupWinrate + " > " + bestFitDeckMatchupWinrate);
                        Log.Info("New Best Fit Deck is" + i + " has matchup winrate " + thisDeckMatchupWinrate + " and overall winrate " + thisDeckWinrate + " and match percent " + matchPercent + "%");


                        bestFitDeckIndex = i;
                        bestFitDeckMatchPercent = matchPercent;
                        bestFitDeckMatchupWinrate = thisDeckMatchupWinrate;
                        bestFitDeckWinrate = thisDeckWinrate;
                    }
                    // else if it has an equal matchup winrate but a greater overall winrate then use this
                    else if (thisDeckMatchupWinrate == bestFitDeckMatchupWinrate && thisDeckWinrate > bestFitDeckWinrate)
                    {
                        Log.Info("Match Percent Equal: " + matchPercent + " = " + bestFitDeckMatchPercent);
                        Log.Info("Matchup Winrate Equal: " + thisDeckMatchupWinrate + " = " + bestFitDeckMatchupWinrate);
                        Log.Info("Overall Winrate Beaten: " + thisDeckWinrate + " > " + bestFitDeckWinrate);
                        Log.Info("New Best Fit Deck is" + i + " has matchup winrate " + thisDeckMatchupWinrate + " and overall winrate " + thisDeckWinrate + " and match percent " + matchPercent + "%");

                        bestFitDeckIndex = i;
                        bestFitDeckMatchPercent = matchPercent;
                        bestFitDeckMatchupWinrate = thisDeckMatchupWinrate;
                        bestFitDeckWinrate = thisDeckWinrate;
                    }
                    
                }

            }

            // return bestFitDeckIndex, bestFitDeckMatchPercent
            Log.Info("Returning: index=" + bestFitDeckIndex + " match=" + bestFitDeckMatchPercent + "%");
            return (bestFitDeckIndex, bestFitDeckMatchPercent, bestFitDeckIndex!=-1);
        }

        // Function for safely getting the winrate for a matchup
        private double GetMatchupWinrate(int playerArchetype, int opponentArchetype)
        {
            // if the matchup exists, return it
            if (_matchups.ContainsKey(playerArchetype) && _matchups[playerArchetype].ContainsKey(opponentArchetype))
            {
                return _matchups[opponentArchetype][playerArchetype];
            }
            // if the matchup doesn't exist, return -1
            else
            {
                return -1;
            }
        }

        // Function to get the players best fit deck so we can get matchups
        private Nullable<int> GetPlayerBestFit()
        {
            // get a list of dbfIds of the cards in the users deck
            List<int> playerCardsDbf = GetDbfIDs(_game.Player.PlayerCardList);
            string deckPlayedCardsDbfIdString = string.Join(", ", playerCardsDbf);

            // get the players best matching archetype
            (int, double, bool) results = GetBestFitDeck(playerCardsDbf, _metaUserClassDecks);
            int playerDeckIndex = results.Item1;
            Nullable<int> playerArchetype = results.Item3 ? (Nullable<int>)_metaUserClassDecks[playerDeckIndex]["archetype_id"] : null;

            return playerArchetype;
        }

        // Function for getting the list of dbfIds in a list of cards, used to query them in APIs
        private List<int> GetDbfIDs(List<Card> cards)
        {
            List<int> dbfIds = new List<int>();
            foreach (Card card in cards)
            {
                // for the count of the card, add the dbfId to the list
                for (int i = 0; i < card.Count; i++)
                    dbfIds.Add(card.DbfId);
            }

            return dbfIds;
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
                string cardHealth = (string)_dbfIdToCardInfo[cardDbfId]["health"];
                string rarity = (string)_dbfIdToCardInfo[cardDbfId]["rarity"];
                string groups = (string)_dbfIdToCardInfo[cardDbfId]["group"];
                // Add the card to the list
                deck.Add(new CardInfo(cardDbfId, cardName, cardCost, cardHealth, cardAttack, cardDescription, cardType, rarity, groups, false)); // false as not been played yet
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
