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

namespace HDT_OpponentGuesser
{
    public class OpponentGuesser
    {
        private static GameV2 _game;
        private static Player _opponent;
        private static string _class;
        private static JToken _metaClassDecks;
        private static double _minimumMatch = 10; // minimum % of cards that must match for a deck to be considered a possible match
        private static bool _firstTurn;

        // Triggered when the game starts
        internal static void GameStart()
        {
            // set the private variables
            _game = Hearthstone_Deck_Tracker.Core.Game;
            _opponent = null; // reset the opponent for this new game
            _class = null; // reset the class for this new game
            _firstTurn = true; // reset the first turn for this new game
            _metaClassDecks = null;
        } 

        // Triggered when a turn starts
        internal static void TurnStart(ActivePlayer player)
        {
            // setting up params on the first turn of the game once opponent has loaded in
            if (_firstTurn) 
            { 
                _opponent = _game.Opponent;
                _class = _opponent.Class;
                _class = _class.ToUpper();
                Log.Info("Start of first turn: opponent class is " + _class);

                #region Do an API call to get a list of all meta decks
                // Create the URL
                string url = "https://hsreplay.net/analytics/query/list_decks_by_win_rate_v2/?GameType=RANKED_STANDARD&LeagueRankRange=GOLD&Region=ALL&TimeRange=CURRENT_PATCH";
                Log.Info("url: " + url);


                // Create the HTTP client
                HttpClient client = new HttpClient();

                // Make the API call
                HttpResponseMessage response = client.GetAsync(url).Result;

                // Get the response content
                HttpContent content = response.Content;

                // Get the string content
                string stringContent = content.ReadAsStringAsync().Result;
                Log.Info("stringContent: " + stringContent);
                content.Dispose();
                client.Dispose();
                #endregion

                #region Getting the list of meta decks for this class
                // Convert the string content to a JSON object
                dynamic jsonContent = JsonConvert.DeserializeObject(stringContent);
                //Log.Info("jsonContent: " + jsonContent);
                Log.Info("jsonContent type: " + jsonContent.GetType());

                // Get the jsonContent.series.data and store it to "allDecks"
                JObject allDecks = jsonContent.series.data;
                Log.Info("All Decks: " + allDecks);
                Log.Info("All Decks type: " + allDecks.GetType());

                // Log the available keys
                Log.Info("All Decks keys: " + allDecks.Properties().Select(p => p.Name));

                // Get the Decks for this class only
                _metaClassDecks = allDecks[_class];
                Log.Info("Meta Decks for this class: " + _metaClassDecks);


                #region Transform deck_list into a 1D array
                // _metaClassDecks[i][deck_list] is in the format "[[cardID, numberInDeck],[cardID, numberInDeck] ...]"
                // we want it in format [cardID, cardID, cardID, ...]

                // for each deck in _metaClassDecks
                for (int i = 0; i < _metaClassDecks.Count(); i++)
                {
                    Log.Info("Getting the deck_list for deck " + i + " ...");
                    // first convert the string to a matrix
                    List<List<int>> deckCardsMatrix = JsonConvert.DeserializeObject<List<List<int>>>(_metaClassDecks[i]["deck_list"].ToString());

                    // then convert the matrix to a 1D list, by adding deckCardsMatrix[i][0] to the list deckCards deckCardsMatrix[i][1] times
                    List<int> deckCards = new List<int>();
                    for (int j = 0; j < deckCardsMatrix.Count(); j++)
                    {
                        for (int k = 0; k < deckCardsMatrix[j][1]; k++)
                        {
                            Log.Info("Adding " + deckCardsMatrix[j][0] + " to the deck ...");
                            deckCards.Add(deckCardsMatrix[j][0]);
                        }
                    }

                    // then replace the deck_list's value with the 1D list
                    _metaClassDecks[i]["deck_list"] = "["+string.Join(",", deckCards)+"]";


                }

                #endregion

                _firstTurn=false;

                #endregion
            }
        }



        // Triggered when the opponent plays a card
        internal static void OpponentPlay(Card card)
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
            List<Card> deckPlayedCards = allPlayedCards.Where(c => !c.IsCreated && card.Collectible).ToList(); // cards must be collectible (eg not a token) and have not been created
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
            Log.Info("deckPlayedCardsDbfId: " + deckPlayedCardsDbfId);

            // Loop through _metaClassDecks and find which has the most cards in common with deckPlayedCards
            int bestFitDeckIndex = -1;
            double bestFitDeckMatchPercent = _minimumMatch; // we are checking for strict improvement, so this value means we only consider guessing decks that match more than this percentage
            double bestWinRate = -1;
            Log.Info("Looping through _metaClassDecks ...");
            for(int i=0; i<_metaClassDecks.Count(); i++)
            {
                Log.Info("Meta Deck " + i + ": " + _metaClassDecks[i]);
                List<int> deckList = JsonConvert.DeserializeObject<List<int>>(_metaClassDecks[i]["deck_list"].ToString());
                int matchCount = 0;
                Log.Info("Decklist is: " + string.Join(", ",deckList));

                // Loop through the deckPlayedCardsDbfId
                foreach (int cardDbfId in deckPlayedCardsDbfId)
                {
                    Log.Info("Checking if " + cardDbfId + " is in the decklist ...");
                    // If this card is in the deckList, increment the matchCount and pop only the first instance of it from the deckList so it can't be matched again
                    if (deckList.Contains(cardDbfId))
                    {
                        Log.Info("Match found!");
                        matchCount++;
                        deckList.Remove(cardDbfId);
                    }
                }

                // Calculate the match percentage and winrate
                double matchPercent = (double)matchCount / (double)deckPlayedCardsDbfId.Count() * 100;
                double winRate = (double)_metaClassDecks[i]["win_rate"];
                Log.Info("Match count: " + matchCount + ", Cards played from deck: " + deckPlayedCardsDbfId.Count()+ " ("+matchPercent+") winrate");

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
                Log.Info("Deck " + i + "("+ _metaClassDecks[i]["deck_id"] + ")"+" has a " + matchPercent + "% match with the cards played, and a " + winRate + "% winrate");
            }
            if(bestFitDeckIndex != -1)
            {
                JToken bestFitDeck = _metaClassDecks[bestFitDeckIndex];
                string archetypeId = bestFitDeck["archetype_id"].ToString();

                // API call toget name of deck with this archetype_id

                #region Getting the name of the Deck
                #region Do an API call to get info on this decks archetype
                // Create the URL
                string url = $"https://hsreplay.net/api/v1/archetypes/{archetypeId}";
                Log.Info("url: " + url);

                // Create the HTTP client
                HttpClient client = new HttpClient();

                // Make the API call
                HttpResponseMessage response = client.GetAsync(url).Result;

                // Get the response content
                HttpContent content = response.Content;

                // Get the string content
                string stringContent = content.ReadAsStringAsync().Result;
                Log.Info("stringContent: " + stringContent);
                content.Dispose();
                client.Dispose();
                #endregion

                #region Getting the archetype name from this info
                // Convert the string content to a JSON object
                dynamic jsonContent = JsonConvert.DeserializeObject(stringContent);
                //Log.Info("jsonContent: " + jsonContent);
                Log.Info("jsonContent type: " + jsonContent.GetType());

                // Get the name from this JSON object
                string bestDeckName = jsonContent["name"];
                #endregion
                #endregion

                Log.Info("Best fit deck is archetype "+bestDeckName+" at index " + bestFitDeckIndex +  " (" + _metaClassDecks[bestFitDeckIndex]["deck_id"] + ") with a " + bestFitDeckMatchPercent + "% match (greater than minimum of " + _minimumMatch + "%) and a " + bestWinRate + "% winrate");

            }
            else
                Log.Info("No deck has a match greater than the minimum of " + _minimumMatch + "%");
            #endregion
        }

    }

    public class OpponentGuesserPlugin: IPlugin
    {
        public void OnLoad()
        {
            // Triggered upon startup and when the user ticks the plugin on

            // Registering the plugin to the game events
            GameEvents.OnGameStart.Add(OpponentGuesser.GameStart);
            GameEvents.OnOpponentPlay.Add(OpponentGuesser.OpponentPlay);
            GameEvents.OnTurnStart.Add(OpponentGuesser.TurnStart);
        }

        public void OnUnload()
        {
            // Triggered when the user unticks the plugin, however, HDT does not completely unload the plugin.
            // see https://git.io/vxEcH
        }

        public void OnButtonPress()
        {
            // Triggered when the user clicks your button in the plugin list
        }

        public void OnUpdate()
        {
            // called every ~100ms
        }

        public string Name => "PLUGIN NAME";

        public string Description => "DESCRIPTION";

        public string ButtonText => "BUTTON TEXT";

        public string Author => "Dmuss";

        public Version Version => new Version(0, 0, 10);

        public MenuItem MenuItem => null;
    }
}
