# Hearthstone DeckTracker - Opponent Guesser Plugin

## Installation instructions

1. Copy .\Hearthstone_OpponentGuesser\Main\HDT_OpponentGuesser\HDT_OpponentGuesser\bin\Debug\HDT_OpponentGuesser.dll to %AppData%\HearthstoneDeckTracker\Plugins
2. Copy the dependent Assemblies dll's to %AppData%\HearthstoneDeckTracker\ (eg C:\Users\USERNAME\AppData\Local\HearthstoneDeckTracker\app-1.20.10\)
    a. .\Hearthstone_OpponentGuesser\Main\HDT_OpponentGuesser\HDT_OpponentGuesser\bin\Debug\System.Memory.dll
        - This gives error when loading dll in HsReplay: The located assembly's manifest definition does not match the assembly reference.
    b. .\Hearthstone_OpponentGuesser\Main\HDT_OpponentGuesser\HDT_OpponentGuesser\bin\Debug\System.Text.Json.dll
        - This works
3. Restart HDT

## Development Notes

### Useful Links

- [Creating a Plugin](https://github.com/HearthSim/Hearthstone-Deck-Tracker/wiki/Creating-Plugins)
- [Basic Plugin Tutorial](https://github.com/HearthSim/Hearthstone-Deck-Tracker/wiki/Basic-plugin-creation-tutorial)
  - [Where to Find The Logs](https://github.com/HearthSim/Hearthstone-Deck-Tracker/wiki/Creating-Plugins#basics-where-to-start)