# Hearthstone DeckTracker - Opponent Guesser Plugin

NOTE: currently this plugin only supports Standard_Ranked, and will not query for Wild, Twist, or other game modes decks.

## Ethics and Motivation

Hearthstone has a broad 'pen and paper' policy. This plugin does not violate this policy given it is simply automating something that can be done manually (e.g. launching hsreplay on a second monitor and filtering decks by class then looking at the most popular decks for a best fit).

## Explanation

This project is a C# plugin for Hearthstone DeckTracker that integrates with HSReplay.net via API calls, allowing users to retrieve valuable information about the predicted opponents deck against the detected users deck archetypes.

Selenium is used to grab a session cookie to allow for user's with premium accounts on HSReplay.net to access premium stats which recquire a valid session cookie in their API calls. 

WPF is used in the GUI elements.

## Video Demonstration

Click To See Video Presentation
[![Watch the video](https://img.youtube.com/vi/E7a-nlvYjV0/maxresdefault.jpg)](https://www.youtube.com/watch?v=E7a-nlvYjV0)
## How to use the plugin

[In Depth Text Explanation Here](./Documents/Manual.md)



## Installation instructions

1. Download the HDT_OpponentGuesser.zip from the latest release
2. Extract the HDT_OpponentGuesser.zips contents to "*%AppData%\HearthstoneDeckTracker\Plugins*"
   1. (Launch file explorer + copy this path into the address bar)
   ![image](./Documents/Images/PluginsFolder.png)
3. Restart HDT

## Suggestions

- If you have any suggestions (bugfixes, improvements, additional features, etc.), please feel free to open an issue on this repo and/or reach out to me and these will be considered for future updates.

## Contributions

- If you would like to contribute, please feel free to open a pull request on this repo
- [Developer Docs](./Documents/DeveloperDocs.md)
