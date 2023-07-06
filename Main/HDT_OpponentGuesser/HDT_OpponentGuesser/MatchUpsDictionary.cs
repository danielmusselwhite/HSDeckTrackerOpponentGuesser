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
        private static Dictionary<int, Dictionary<int, double>> _matchups;

        // Creating Dictionary of all cards in game for efficient lookup
        public static Dictionary<int, Dictionary<int, double>> GetMatchUpsDictionary()
        {
            if (_matchups == null)
            {
                _matchups = new Dictionary<int, Dictionary<int, double>>() { };

                // Making API call to get info on all cards via api
                var httpClient = new HttpClient();
                var response = httpClient.GetAsync("https://hsreplay.net/analytics/query/head_to_head_archetype_matchups_v2/?GameType=RANKED_STANDARD&LeagueRankRange=GOLD&TimeRange=CURRENT_PATCH").Result;
                HttpContent content = response.Content;
                string stringContent = content.ReadAsStringAsync().Result;

                // Convert the string content to a JSON object
                dynamic jsonContent = JsonConvert.DeserializeObject(stringContent);
                var data = jsonContent.series.data;

                // for each user deck in the data
                foreach(JProperty user in data)
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

            }

            return _matchups;
        }
    }
}