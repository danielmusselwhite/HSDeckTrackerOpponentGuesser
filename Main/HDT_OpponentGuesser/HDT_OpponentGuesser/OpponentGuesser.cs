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

        // Triggered when the game starts
        internal static async void GameStart()
        {
            // set the private variables
            _game = Hearthstone_Deck_Tracker.Core.Game;
        } 

        // Triggered when a turn starts
        internal static void TurnStart(ActivePlayer player)
        {
            // if the private variables are not yet set, set them
            if (_opponent == null)
            {
                _opponent = _game.Opponent;
            }
            if (_class == null)
            {
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
                
                // Testing indexing the meta decks
                Log.Info("Meta Deck 1: " + _metaClassDecks[0]);
                Log.Info("Meta Deck 1 info: Deck ID = " + _metaClassDecks[0]["deck_id"] + "Win Rate = " + _metaClassDecks[0]["win_rate"]);





                #endregion

            }
        }



        // Triggered when the opponent plays a card
        internal static void OpponentPlay(Card card)
        {
            #region Getting the list of cards the opponent played that originated from their deck
            // Log some core info on the card that was just played
            Log.Info($"Opponent played {card.Name} ({card.Id}), {card.Cost}");

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

        public Version Version => new Version(0, 0, 5);

        public MenuItem MenuItem => null;
    }
}
