using Hearthstone_Deck_Tracker.Utility.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HDT_OpponentGuesser
{
    internal class HsrSessionCookieGetter
    {
        // function for getting the session cookie, to be used in (null if they aren't logged in)
        public static string GetSessionCookie()
        {
            // getting the session cookie from the hsreplay.net website
            var cookieContainer = new System.Net.CookieContainer();
            var handler = new System.Net.Http.HttpClientHandler() { CookieContainer = cookieContainer };
            var httpClient = new HttpClient(handler);
            var response = httpClient.GetAsync("https://hsreplay.net/").Result;
            response.EnsureSuccessStatusCode();
            var cookies = cookieContainer.GetCookies(new Uri("https://hsreplay.net/")).Cast<System.Net.Cookie>();
            var sessionCookie = cookies.FirstOrDefault(x => x.Name == "sessionid");
            return sessionCookie != null ? sessionCookie.Value : null;
        }

        // function using the sessionCookie to test if this is a premium account or not
        public static bool IsPremium()
        {
            string sessionCookie = GetSessionCookie();
            bool isPremium = false;
            if (sessionCookie != null)
            {
                var cookieContainer = new System.Net.CookieContainer();
                var handler = new System.Net.Http.HttpClientHandler() { CookieContainer = cookieContainer };
                var httpClient = new HttpClient(handler);
                httpClient.DefaultRequestHeaders.Add("Cookie", "sessionid=" + sessionCookie);
                HttpResponseMessage response = httpClient.GetAsync("https://hsreplay.net/api/v1/account/").Result;
                response.EnsureSuccessStatusCode();
                HttpContent content = response.Content;
                string stringContent = content.ReadAsStringAsync().Result;
                dynamic jsonContent = JsonConvert.DeserializeObject(stringContent);
                isPremium = jsonContent.is_premium;
            }
            return isPremium;
        }
    }
}
