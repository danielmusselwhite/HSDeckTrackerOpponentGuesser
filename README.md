# Hearthstone DeckTracker - Opponent Guesser Plugin

NOTE: currently this plugin only supports Standard_Ranked, and will not query for Wild, Twist, or other game modes decks.

## Explanation of the plugin

<!-- Table with 1 row with 2 columns -->
| ![InGameView](./Documents/Images/InGameView_F.png) | ![InGameView](./Documents/Images/InGameView_T.png) |
|----|----|
| Played Cards Toggle Off | Played Cards Toggle On |



- Screenshots above shows the plugin in action
- A grey box appears in the bottom left of the screen
  - If no Meta Deck from HSReplay matches the opponents played cards over n%.
  - If a Meta Deck has been found, it will show the name of the deck (and the first few characters of its ID to differnetiate decks with the same name), winrate of the deck, and the decks % match.
- if you click the "View Deck" button, it will launch the HSReplay page for that deck in your default browser
- if you hover over the "View Deck" button, it will display in game the decklist of that deck; colour coding the types of cards for easy viewing
  - Red = Minion
  - Blue = Spell
  - Yellow = Weapon
  - Green = Location
  - Magenta = Secret
  - Star next to name = Legendary
- Hovering over a card will then display information on that card (e.g. name, cost, type, rarity, (attack and damage for minions), etc.)
- This updates in real time, every time the opponent plays a card
- Toggleable button for viewing the played cards vs cards remaining:
  - When False (Red) - shows the complete deck list we are predicting opponent is using
  - When True (Green) - modifies this decklist to add in a darker colour the cards opponents have played that exist in the decklist + reducing the count of that card

## Installation instructions

1. Download the HDT_OpponentGuesser.zip from the latest release
2. Extract the HDT_OpponentGuesser.dll from the zip folder to %AppData%\HearthstoneDeckTracker\Plugins
3. Restart HDT

## Suggestions

- If you have any suggestions (bugfixes, improvements, additional features, etc.), please feel free to open an issue on this repo and/or reach out to me and these will be considered for future updates.

## Contributions

- If you would like to contribute, please feel free to open a pull request on this repo
- [Developer Docs](./Documents/DeveloperDocs.md)
