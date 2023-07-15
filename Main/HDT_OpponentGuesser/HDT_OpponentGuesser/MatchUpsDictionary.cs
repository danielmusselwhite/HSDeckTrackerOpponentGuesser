using Hearthstone_Deck_Tracker.Utility.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HDT_OpponentGuesser
{
    // Singleton to get the MatchUps - only want to make the API call once as it is large
    internal static class MatchUpsDictionary
    {

        private static Dictionary<int, Dictionary<int, double>> _matchups = null;
        private static string _lastRank = null;

        // Creating Dictionary of all cards in game for efficient lookup
        public static Dictionary<int, Dictionary<int, double>> GetMatchUpsDictionary(string rankRange)
        {
            if (_matchups == null || rankRange != _lastRank)
            {
                // try to get the matchups from the API; if it fails e.g. (most likely) as API is experiencng down time just return null
                // IE in OpponentGusesser is _matchups == null, it will fail on the query and so use the opponents winrate overall instead of their matchup against us
                // Will then retry in the next game in case the API endpoint is up again
                try
                {
                    Log.Info((_matchups == null ? "Matchups is null ": "Rank has changed from "+_lastRank+" to "+rankRange)+" so creating and populating matchups dict");
                    _matchups = new Dictionary<int, Dictionary<int, double>>() { };

                    #region Making API call to get info on all cards via api
                    HttpResponseMessage response;
                    // if a new patch, then current_patch will fail so try first
                    try
                    {
                        response = GetMatchUpsResult(rankRange, true);
                        if (response.Content == null)
                        {
                            throw new Exception("response.Content was null");
                        }
                    }
                    // if it fails, do same query but remove current_patch filter
                    catch
                    {
                        Log.Info("Getting matchups for current_patch failed (likely a new patch and backend hasn't been updated yet; trying for all patches");
                        response = GetMatchUpsResult(rankRange, false);
                    }
                    #endregion



                    // Converting to string content then to a parseable JSON object
                    HttpContent content = response.Content;
                    string stringContent = content.ReadAsStringAsync().Result;
                    dynamic jsonContent = JsonConvert.DeserializeObject(stringContent);
                    var data = jsonContent.series.data;

                    // for each user deck in the data
                    foreach (JProperty user in data)
                    {
                        int userArch = Int32.Parse(user.Name);

                        //dictionary for this archs matchups
                        Dictionary<int, double> thisMatchups = new Dictionary<int, double>();

                        // for each opponent deck match up against this user deck
                        foreach (JProperty opp in user.Value)
                        {
                            int oppArch = Int32.Parse(opp.Name);
                            double winrate = Double.Parse((string)opp.Value["win_rate"]);
                            thisMatchups.Add(oppArch, winrate);
                        }

                        _matchups.Add(userArch, thisMatchups);

                    }

                    _lastRank = rankRange;
                    Log.Info("Successfully created and populated matchups for rank: "+ rankRange);
                }
                catch
                {
                    // API failed (likely due to downtime), so return null so that the opponent guesser will use the opponents overall winrate instead of their matchup against us
                    // Can retry next game in case API is back up
                    _matchups = null;
                    Log.Info("Failed to create and populate matchups");
                }

            }

            Log.Info("Returning _matchups");
            Log.Info("matchups first = " + _matchups[-14][-10]);
            return _matchups;
        }

        // function for getting the httpResponseMessage for the API call parameterized
        private static HttpResponseMessage GetMatchUpsResult(string rankRange, bool currentPatch)
        {
            string currentPatchString = currentPatch ? "&TimeRange=CURRENT_PATCH" : "";

            string sessionCookie = HsrSessionCookieGetter.GetSessionCookie();
            var cookieContainer = new System.Net.CookieContainer();
            var handler = new System.Net.Http.HttpClientHandler() { CookieContainer = cookieContainer };
            var httpClient = new HttpClient(handler);
            if(sessionCookie != null) 
                httpClient.DefaultRequestHeaders.Add("Cookie", "sessionid=" + sessionCookie);
            HttpResponseMessage response = httpClient.GetAsync($"https://hsreplay.net/analytics/query/head_to_head_archetype_matchups_v2/?GameType=RANKED_STANDARD&LeagueRankRange={rankRange}{currentPatchString}").Result;
            response.EnsureSuccessStatusCode();
            return response;
        }

    }
}