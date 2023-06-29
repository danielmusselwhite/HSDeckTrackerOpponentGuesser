# Hearthstone DeckTracker - Opponent Guesser Plugin

# Installation instructions

1. Copy .\Hearthstone_OpponentGuesser\Main\HDT_OpponentGuesser\HDT_OpponentGuesser\bin\Debug\HDT_OpponentGuesser.dll to %AppData%\HearthstoneDeckTracker\Plugins
2. Copy the dependent Assemblies dll's (that do not already exist in HearthstoneDeckTracker) from Project resources to %AppData%\HearthstoneDeckTracker\ (eg C:\Users\USERNAME\AppData\Local\HearthstoneDeckTracker\app-1.20.10\)
   1. Currently have none due to rearchitecting the project
3. Restart HDT

# Development Notes

## Debugging

**If you get anything in log of hsreplay saying "The located assembly's manifest definition does not match the assembly reference."**
- Just use Nuget to download the version it is wanting in the reference, then copy that dll to the HDT folder
  - Can either be done via Package Manager Console (harder) or in VS Code by right clicking on the project and selecting "Manage Nuget Packages" (easier)
## Useful Links

- [Creating a Plugin](https://github.com/HearthSim/Hearthstone-Deck-Tracker/wiki/Creating-Plugins)
- [Basic Plugin Tutorial](https://github.com/HearthSim/Hearthstone-Deck-Tracker/wiki/Basic-plugin-creation-tutorial)
  - [Where to Find The Logs](https://github.com/HearthSim/Hearthstone-Deck-Tracker/wiki/Creating-Plugins#basics-where-to-start)