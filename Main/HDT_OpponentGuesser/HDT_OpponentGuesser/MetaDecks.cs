using Hearthstone_Deck_Tracker.Utility.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HDT_OpponentGuesser
{
    internal class MetaDecks
    {

        private static string _allMetaDecks = "";

        // Function to do an API call to get a list of all meta decks
        public static string GetAllMetaDecks()
        {
            HttpClient client;
            HttpResponseMessage response;
            try
            {
                client = new HttpClient();
                response = client.GetAsync("https://hsreplay.net/analytics/query/list_decks_by_win_rate_v2/?GameType=RANKED_STANDARD&LeagueRankRange=GOLD&Region=ALL&TimeRange=CURRENT_PATCH").Result;
                response.EnsureSuccessStatusCode();
                if (response.Content == null)
                {
                    throw new Exception("response.Content was null");
                }
            }
            // if it fails, do same query but remove current_patch filter
            catch
            {
                Log.Info("Getting matchups for current_patch failed (likely a new patch and backend hasn't been updated yet; trying for all patches");
                client = new HttpClient();
                response = client.GetAsync("https://hsreplay.net/analytics/query/list_decks_by_win_rate_v2/?GameType=RANKED_STANDARD&LeagueRankRange=GOLD&Region=ALL").Result;
            }

            // Get the string content
            HttpContent content = response.Content;

            _allMetaDecks = content.ReadAsStringAsync().Result;
            return _allMetaDecks;
        }
    
    
        // Function to filter _allMetaDecks to only include decks belonging to a specified class
        public static JToken GetClassMetaDecks(string thisClass)
        {
            //region Parsing the _allMetaDecks JSON String to get the list of meta decks for this class
            
            // Convert the string content to a JSON object
            string stringContent = _allMetaDecks;
            dynamic jsonContent = JsonConvert.DeserializeObject(stringContent);

            // Get the jsonContent.series.data and store it to "allDecks"
            JObject allDecks = jsonContent.series.data;

            // Get the Decks for this class only
            JToken metaClassDecks = TransformDeckListTo1D(allDecks[thisClass]);

            return metaClassDecks;
        }

        // metaClassDecks[i][deck_list] is in the format "[[cardID, numberInDeck],[cardID, numberInDeck] ...]"
        // we want it in format [cardID, cardID, cardID, ...]
        private static JToken TransformDeckListTo1D(JToken metaClassDecks)
        {
            

            // for each deck in metaClassDecks
            for (int i = 0; i < metaClassDecks.Count(); i++)
            {
                // first convert the string to a matrix
                List<List<int>> deckCardsMatrix = JsonConvert.DeserializeObject<List<List<int>>>(metaClassDecks[i]["deck_list"].ToString());

                // then convert the matrix to a 1D list, by adding deckCardsMatrix[i][0] to the list deckCards deckCardsMatrix[i][1] times
                List<int> deckCards = new List<int>();
                for (int j = 0; j < deckCardsMatrix.Count(); j++)
                {
                    for (int k = 0; k < deckCardsMatrix[j][1]; k++)
                    {
                        deckCards.Add(deckCardsMatrix[j][0]);
                    }
                }

                // then replace the deck_list's value with the 1D list
                metaClassDecks[i]["deck_list"] = "[" + string.Join(",", deckCards) + "]";


            }

            return metaClassDecks;
        }

    }


}
