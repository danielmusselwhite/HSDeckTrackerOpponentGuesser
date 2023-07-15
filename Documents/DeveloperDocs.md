
# Development Notes

## Explanation of Architecture and Choices


### Diagrams

#### Entity Relationship Diagram

![ERD](Images/EntityRelationshipDiagram.png)

Diagram explaining the relationships between the different classes and their properties

## Useful notes for development

### Creating GUI Elements

- The GUI elements in this project are WPF UserControls
- So to add a new one, simply right click on project and select Add -> User Control (WPF) and name it
    - This will create a .xaml (frontend) and .xaml.cs (backend) file for you

### Using non-standard Nuget Packages/Assemblies/Libraries

- Current version does not use any, but if any changes require them, here is how to use them.
- In order for them to be used in the plugin, they must be copied to the HearthStoneDeckTracker folder
- This can be done by:
    1. Adding the Nuget Package to the project
    2. Going into the Debug folder after the build, and copying the dll's from the Nuget Packages you installed
    3. Navigating to %AppData%\HearthstoneDeckTracker\ and pasting the dll's into the folder
 
### Testing

- Build the project
- Navigate to: '.\HDT_OpponentGuesser\bin\Debug\'
- Find the 'HDT_OpponentGuesser.dll' inside
- Close Hearthstone Deck Tracker
- Copy over the .dll to '%AppData%\HearthstoneDeckTracker\Plugins'
- Relaunch Hearthstone Deck Tracker

## Possible Future Directions

- Capturing and storing stats of players decks vs different deck archetypes
- Perhaps also using this to find best fit deck for players deck to display their best mulligan choices at game start?
- Currently only supports Standard_Ranked; could be expanded to get the players game mode on game start and modify the API call to pass that in, instead of defaulting to Standard_Ranked
- Potentially also display the cards which don't match the predicted deck in the decklist, as wwas in the original design
    - Didn't end up being incorporated because I thought it could clutter it too much and wasn#t too useful to display
- **Instead of defaulting to GOLD for the queries, have it query the Games rank, then query for that rank (maxxing out at gold if user has non-premium account)**


## Debugging

Logs are stored here: ('%AppData%\HearthstoneDeckTracker\Logs\')

**If you get anything in log of hsreplay saying "The located assembly's manifest definition does not match the assembly reference."**
- Just use Nuget to download the version it is wanting in the reference, then copy that dll to the HDT folder
  - Can either be done via Package Manager Console (harder) or in VS Code by right clicking on the project and selecting "Manage Nuget Packages" (easier)
  - To find the version you are using, right click on the .dll and select properties, then look at the version number on the details tab
## Useful Links

- [Creating a Plugin](https://github.com/HearthSim/Hearthstone-Deck-Tracker/wiki/Creating-Plugins)
- [Basic Plugin Tutorial](https://github.com/HearthSim/Hearthstone-Deck-Tracker/wiki/Basic-plugin-creation-tutorial)
  - [Where to Find The Logs](https://github.com/HearthSim/Hearthstone-Deck-Tracker/wiki/Creating-Plugins#basics-where-to-start)
