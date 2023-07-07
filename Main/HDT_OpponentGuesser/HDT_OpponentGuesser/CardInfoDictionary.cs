using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HDT_OpponentGuesser
{
    // Singleton to get the CardInfo - only want to make the API call once as it is large
    internal static class CardInfoDictionary
    {
        private static Dictionary<int, Dictionary<string, string>> _cardInfo = null;

        // Creating Dictionary of all cards in game for efficient lookup
        public static Dictionary<int, Dictionary<string, string>> GetCardDictionary()
        {
            if (_cardInfo == null)
            {
                _cardInfo = new Dictionary<int, Dictionary<string, string>>() { };

                // Making API call to get info on all cards via api
                var httpClient = new HttpClient();
                var response = httpClient.GetAsync("https://api.hearthstonejson.com/v1/latest/enUS/cards.json").Result;
                HttpContent content = response.Content;
                string stringContent = content.ReadAsStringAsync().Result;

                // Convert the string content to a JSON object
                dynamic jsonContent = JsonConvert.DeserializeObject(stringContent);

                // For each card in the jsonContent, add the relevant details to the Dictionary
                foreach (var card in jsonContent)
                {
                    int dbfId = (int)card.dbfId;
                    string name = (string)card.name;
                    string cost = (string)card.cost;
                    string type = (string)card.type;
                    string health = "";
                    string attack = "";
                    string description = "";
                    string rarity = (string)card.rarity;
                    if (type != null)
                    {
                        if (type.ToUpper() == "SPELL" && card.mechanics != null && card.mechanics.ToString().Contains("SECRET"))
                            type = "SECRET";
                        else if (type.ToUpper() == "MINION")
                        {
                            health = (string)card.health;
                            attack = (string)card.attack;
                        }
                    }
                    if (card.text != null)
                    {
                        description = (string)card.text;
                    }
                    _cardInfo.Add(dbfId, new Dictionary<string, string>() { { "cost", cost }, { "name", name }, { "health", health }, { "attack", attack }, { "description", description }, { "type", type }, { "rarity", rarity } });
                }
            }

            return _cardInfo;
        }
    }
}