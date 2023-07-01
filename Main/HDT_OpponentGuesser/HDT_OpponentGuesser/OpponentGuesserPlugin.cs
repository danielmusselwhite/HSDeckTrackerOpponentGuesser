using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace HDT_OpponentGuesser
{
    public class OpponentGuesserPlugin : IPlugin
    {

        private BestFitDeckDisplay _bfdDisplay; // reference to the main GUI class for displaying this in the HDT overlay 

        public void OnLoad()
        {
            // Triggered upon startup and when the user ticks the plugin on

            // Create the GUI
            _bfdDisplay = new BestFitDeckDisplay();
            Core.OverlayCanvas.Children.Add(_bfdDisplay);

            // Creating an instance of the OpponentGuesser class
            OpponentGuesser opponentGuesser = new OpponentGuesser(_bfdDisplay);

            // Registering the plugin to the game events
            GameEvents.OnGameStart.Add(opponentGuesser.GameStart);
            GameEvents.OnOpponentPlay.Add(opponentGuesser.OpponentPlay);
            GameEvents.OnTurnStart.Add(opponentGuesser.TurnStart);
            GameEvents.OnInMenu.Add(opponentGuesser.InMenu);

        }

        public void OnUnload()
        {
            // Triggered when the user unticks the plugin, however, HDT does not completely unload the plugin.

            // Remove the GUI
            Core.OverlayCanvas.Children.Remove(_bfdDisplay);
        }

        public void OnButtonPress()
        {
            // Triggered when the user clicks your button in the plugin list
        }

        public void OnUpdate()
        {
            // called every ~100ms

            _bfdDisplay.HandleMouseOver();
        }

        public string Name => "Opponent Deck Guesser";

        public string Description => "Guesses what the opponent deck is via using API calls to the meta decks on hsreplay";

        public string ButtonText => "press me to activate!";

        public string Author => "Dmuss";

        public Version Version => new Version(0, 1, 5);

        public MenuItem MenuItem => null;

    }
}
