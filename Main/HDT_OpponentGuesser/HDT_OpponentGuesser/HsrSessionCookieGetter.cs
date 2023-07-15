using Hearthstone_Deck_Tracker.Utility.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace HDT_OpponentGuesser
{
    internal class HsrSessionCookieGetter
    {
        // function for getting the session cookie, to be used in (null if they aren't logged in)
        public static string GetSessionCookie()
        {
            Log.Info("trying to get session cookie");

            // Launch the preferred browser
            IWebDriver driver = new ChromeDriver();

            // Open hsreplay.net/login
            driver.Navigate().GoToUrl("https://hsreplay.net/login");

            // Wait for user to login
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            // wait for url to change to hsreplay.net/welcome
            wait.Until(ExpectedConditions.UrlContains("hsreplay.net/welcome"));

            // Get the sessionid cookie after login
            var sessionIdCookie = driver.Manage().Cookies.GetCookieNamed("sessionid");

            // Extract the value of the sessionid cookie
            string sessionId = sessionIdCookie?.Value;

            // Output the sessionid
            if (sessionId != null)
            {
                Log.Info("Session ID: " + sessionId);
                driver.Quit();
                return sessionId;
            }
            else
            {
                Log.Info("Failed to retrieve session ID.");
                driver.Quit();
                return null;
            }
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
