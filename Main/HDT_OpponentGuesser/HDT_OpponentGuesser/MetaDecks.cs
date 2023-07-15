using Hearthstone_Deck_Tracker.Utility.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace HDT_OpponentGuesser
{
    internal class MetaDecks
    {

        private static string _allMetaDecks = "";
        private static string _lastRank = "";

        // Function to do an API call to get a list of all meta decks
        public static string GetAllMetaDecks(string rankRange)
        {
            if (_allMetaDecks == null || rankRange != _lastRank)
            {
                try
                {

                    Log.Info((_allMetaDecks == null ? "AllMetaDecks is null " : "Rank has changed from " + _lastRank + " to " + rankRange) + " so creating and populating AllMetaDecks dict");

                    // if a new patch, then current_patch will fail so try first
                    HttpResponseMessage response;
                    try
                    {
                        response = GetAllMetaDecksResult(rankRange, true);
                        if (response.Content == null)
                        {
                            throw new Exception("response.Content was null");
                        }
                    }
                    // if it fails, do same query but remove current_patch filter
                    catch
                    {
                        Log.Info("Getting matchups for current_patch failed (likely a new patch and backend hasn't been updated yet; trying for all patches");
                        response = GetAllMetaDecksResult(rankRange, false);
                    }

                    // Get the string content
                    HttpContent content = response.Content;

                    _allMetaDecks = content.ReadAsStringAsync().Result;
                    _lastRank = rankRange;
                    Log.Info("Successfully created and populated _allMetaDecks for rank: " + rankRange);
                }
                catch
                {
                    // API failed (likely due to downtime), so return null so that the opponent guesser will use the opponents overall winrate instead of their matchup against us
                    // Can retry next game in case API is back up
                    _allMetaDecks = null;
                    Log.Info("Failed to create and populate allMetaDecks");
                }
                
            }

            return _allMetaDecks;
        }

        // function for getting the httpResponseMessage for the API call parameterized
        private static HttpResponseMessage GetAllMetaDecksResult(string rankRange, bool currentPatch)
        {
            string currentPatchString = currentPatch ? "&TimeRange=CURRENT_PATCH" : "";

            string sessionCookie = HsrSessionCookieGetter.GetSessionCookie();
            var cookieContainer = new System.Net.CookieContainer();
            var handler = new System.Net.Http.HttpClientHandler() { CookieContainer = cookieContainer };
            var httpClient = new HttpClient(handler);
            if (sessionCookie != null)
                httpClient.DefaultRequestHeaders.Add("Cookie", "sessionid=" + sessionCookie);
            HttpResponseMessage response = httpClient.GetAsync($"https://hsreplay.net/analytics/query/list_decks_by_win_rate_v2/?GameType=RANKED_STANDARD&LeagueRankRange={rankRange}{currentPatchString}").Result;
            response.EnsureSuccessStatusCode();
            return response;
        }


        // Function to filter _allMetaDecks to only include decks belonging to a specified class
        public static JToken GetClassMetaDecks(string thisClass, string rankRange)
        {
            //region Parsing the _allMetaDecks JSON String to get the list of meta decks for this class
            
            // Convert the string content to a JSON object
            string stringContent = GetAllMetaDecks(rankRange);
            dynamic jsonContent = JsonConvert.DeserializeObject(stringContent);

            // Get the jsonContent.series.data and store it to "allDecks"
            JObject allDecks = jsonContent.series.data;

            // Get the Decks for this class only
            JToken metaClassDecks = TransformDeckListTo1D(allDecks[thisClass]);

            return metaClassDecks;
        }

        // Function for getting the name of the decks archetype
        public static string GetDeckArchetypeName(string archetypeId, string className)
        {
            #region Do an API call to get info on this decks archetype
            // Create the URL
            string url = $"https://hsreplay.net/api/v1/archetypes/{archetypeId}";

            // Create the HTTP client
            HttpClient client = new HttpClient();

            // Make the API call
            HttpResponseMessage response = client.GetAsync(url).Result;

            // Get the response content
            HttpContent content = response.Content;

            // Get the string content
            string stringContent = content.ReadAsStringAsync().Result;
            content.Dispose();
            client.Dispose();
            #endregion

            #region Getting the archetype name from this info
            // Convert the string content to a JSON object
            dynamic jsonContent = JsonConvert.DeserializeObject(stringContent);

            // Get the name from this JSON object
            string archetypeName = jsonContent["name"];

            // if there is no name, then use the class name instead
            if (archetypeName == null)
            {
                archetypeName = className + " deck";
            }
            #endregion

            return archetypeName;
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
