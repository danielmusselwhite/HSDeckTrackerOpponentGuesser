# Hearthstone DeckTracker - Opponent Guesser Plugin

# Installation instructions

1. Copy .\Hearthstone_OpponentGuesser\Main\HDT_OpponentGuesser\HDT_OpponentGuesser\bin\Debug\HDT_OpponentGuesser.dll to %AppData%\HearthstoneDeckTracker\Plugins
2. Copy the dependent Assemblies dll's (that do not already exist in HearthstoneDeckTracker) from Project resources to %AppData%\HearthstoneDeckTracker\ (eg C:\Users\USERNAME\AppData\Local\HearthstoneDeckTracker\app-1.20.10\)
   1. Currently have none due to rearchitecting the project
3. Restart HDT

# Development Notes

## TODO's - Core

- FEATURE: On hovering over a card in the decklist, popup with detailed info on it
- FEATURE: Make decklist canvas fit the height of its contents
- FEATURE: Sort the cards in the decklist by mana cost
## TODO's - Maybes

- Feature: Given we are detecting opponent's deck, may be interesting to collate the data to get in-depth W/L vs Deck Archetype not just vs Class
    - E.g. instead of showing that decks WR (grabbed from hsreplay) in the GUI
    - Show: 1. Your WinRate vs Decks of that archetype and 2. The WinRate of that archetype vs your deck
- Feature: Could likely adapt what I use to find best fit of opponents deck, to  find the best fit of users deck to then display the mulligan guide for them on turn 1?
- Feature: Have it detect game mode and send API requests for that mode (e.g. Standard vs Wild) - honestly okay with it just doing ranked_standard for now though as its most popular



## Debugging

**If you get anything in log of hsreplay saying "The located assembly's manifest definition does not match the assembly reference."**
- Just use Nuget to download the version it is wanting in the reference, then copy that dll to the HDT folder
  - Can either be done via Package Manager Console (harder) or in VS Code by right clicking on the project and selecting "Manage Nuget Packages" (easier)
## Useful Links

- [Creating a Plugin](https://github.com/HearthSim/Hearthstone-Deck-Tracker/wiki/Creating-Plugins)
- [Basic Plugin Tutorial](https://github.com/HearthSim/Hearthstone-Deck-Tracker/wiki/Basic-plugin-creation-tutorial)
  - [Where to Find The Logs](https://github.com/HearthSim/Hearthstone-Deck-Tracker/wiki/Creating-Plugins#basics-where-to-start)