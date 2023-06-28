﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Adding the hearthstone deck tracker references required for creating the plugin
using System.Windows.Controls;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker.Plugins;
using Hearthstone_Deck_Tracker.Utility.Logging;


namespace HDT_OpponentGuesser
{
    public class OpponentGuesser
    {
        private static GameV2 _game;

        // Triggered when the game starts
        internal static void GameStart()
        {
            // set _game to  Hearthstone_Deck_Tracker.Core.Game;
            _game = Hearthstone_Deck_Tracker.Core.Game;
        }

        // Triggered when the opponent plays a card
        internal static void OpponentPlay(Card card)
        {
            // Log some core info on the card that was just played
            Log.Info($"Opponent played {card.Name} ({card.Id}), {card.Cost}");

            // Get the List of all cards played
            List<Card> allPlayedCards = _game.Opponent.OpponentCardList;
            allPlayedCards.Add(card);
            string allPlayedCardsString = string.Join(", ", allPlayedCards.Select(c => c.Name));
            Log.Info($"Opponent has played {allPlayedCards.Count()} cards in total ({allPlayedCardsString})");
            

            // Filter by cards that originated from their deck
            List<Card> deckPlayedCards = allPlayedCards.Where(c => !c.IsCreated && card.Collectible).ToList(); // cards must be collectible (eg not a token) and have not been created
            string deckPlayedCardsString = string.Join(", ", deckPlayedCards.Select(c => c.Name));
            Log.Info($"Opponent has played {deckPlayedCards.Count()} cards from their deck in total ({deckPlayedCardsString})");

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

        public Version Version => new Version(0, 0, 2);

        public MenuItem MenuItem => null;
    }
}
