
# Development Notes

## Explanation of Architecture and Choices


## Diagrams


## Useful notes for development


## Possible Future Directions

- Capturing and storing stats of players decks vs different deck archetypes
- Perhaps also using this to find best fit deck for players deck to display their best mulligan choices at game start?
- Currently only supports Standard_Ranked; could be expanded to get the players game mode on game start and modify the API call to pass that in, instead of defaulting to Standard_Ranked


## Debugging

**If you get anything in log of hsreplay saying "The located assembly's manifest definition does not match the assembly reference."**
- Just use Nuget to download the version it is wanting in the reference, then copy that dll to the HDT folder
  - Can either be done via Package Manager Console (harder) or in VS Code by right clicking on the project and selecting "Manage Nuget Packages" (easier)
## Useful Links

- [Creating a Plugin](https://github.com/HearthSim/Hearthstone-Deck-Tracker/wiki/Creating-Plugins)
- [Basic Plugin Tutorial](https://github.com/HearthSim/Hearthstone-Deck-Tracker/wiki/Basic-plugin-creation-tutorial)
  - [Where to Find The Logs](https://github.com/HearthSim/Hearthstone-Deck-Tracker/wiki/Creating-Plugins#basics-where-to-start)