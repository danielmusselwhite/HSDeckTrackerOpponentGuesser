# Hearthstone DeckTracker - Opponent Guesser Plugin

NOTE: currently this plugin only supports Standard_Ranked, and will not query for Wild, Twist, or other game modes decks.

## Explanation of the plugin

![InGameView](./Documents/Images/InGameView.png)

- Screenshot above shows the plugin in action
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

## Installation instructions

0. Download the HDT_OpponentGuesser.dll from the latest release
1. Copy HDT_OpponentGuesser.dll to %AppData%\HearthstoneDeckTracker\Plugins
2. Copy the dependent Assemblies dll's (that do not already exist in HearthstoneDeckTracker) from Project resources to %AppData%\HearthstoneDeckTracker\ (eg C:\Users\USERNAME\AppData\Local\HearthstoneDeckTracker\app-1.20.10\)
   1. Currently have none due to rearchitecting the project
3. Restart HDT

## Suggestions

- If you have any suggestions (bugfixes, improvements, additional features, etc.), please feel free to open an issue on this repo and/or reach out to me and these will be considered for future updates.

## Contributions

- If you would like to contribute, please feel free to open a pull request on this repo
