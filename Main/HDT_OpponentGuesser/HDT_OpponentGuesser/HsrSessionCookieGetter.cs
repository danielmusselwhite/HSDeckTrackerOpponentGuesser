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
using Hearthstone_Deck_Tracker.FlyoutControls.Options.HSReplay;
using System.Net;

namespace HDT_OpponentGuesser
{


    internal static class HsrSessionCookieGetter
    {

        public static string _sessionId = null;

        // function for getting the session cookie, to be used in (null if they aren't logged in)
        public static string GetSessionCookie()
        {
            
            try
            {
                if (_sessionId == null)
                {
                    Log.Info("trying to get session cookie");

                    string sessionId = null;

                    // Launch the preferred browser
                    IWebDriver driver = new ChromeDriver();

                    // Open hsreplay.net/login
                    driver.Navigate().GoToUrl("https://hsreplay.net/login");

                    // Wait for user to login
                    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(120));
                    // wait for url to change to hsreplay.net/welcome
                    wait.Until(ExpectedConditions.UrlContains("hsreplay.net/welcome"));

                    // Get the sessionid cookie after login
                    var sessionIdCookie = driver.Manage().Cookies.GetCookieNamed("sessionid");

                    // Extract the value of the sessionid cookie
                    sessionId = sessionIdCookie?.Value;

                    // Output the sessionid
                    if (sessionId != null)
                    {
                        Log.Info("Session ID: " + sessionId);
                    }
                    else
                    {
                        Log.Info("Failed to retrieve session ID.");
                        return null;
                    }
                    driver.Quit();
                    _sessionId = sessionId;
                }
                return _sessionId;
            }
            catch
            {
                Log.Info("Failed to retrieve session ID.");
                return null;
            }

        }

        // function using the sessionCookie to test if this is a premium account or not
        public static bool IsPremium()
        {
            string sessionCookie = _sessionId;
            bool isPremium = false;
            if (sessionCookie != null)
            {
                // Create the HTTP request
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://hsreplay.net/api/v1/account/");
                request.Method = "GET";

                // Add the session ID as a cookie
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(new System.Net.Cookie("sessionid", _sessionId) { Domain = "hsreplay.net" });

                // Send the request and get the response
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    using (var stream = response.GetResponseStream())
                    {
                        using (var reader = new System.IO.StreamReader(stream))
                        {
                            string responseJson = reader.ReadToEnd();

                            // Assuming the response is in JSON format, you can parse it using a JSON library
                            // For example, using Newtonsoft.Json:
                            dynamic jsonObject = Newtonsoft.Json.JsonConvert.DeserializeObject(responseJson);
                            isPremium = jsonObject["is_premium"];
                        }
                    }
                }

                Log.Info("isPremium: " + isPremium);
            }
            return isPremium;
        }
    }
}
