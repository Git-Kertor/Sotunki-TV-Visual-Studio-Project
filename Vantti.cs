using System.Collections.Generic;
using HtmlAgilityPack;
using System.Linq;
using System;

namespace Sotunki_TV
{
    // This class connects to Vantti (school resturant provider) and scrapes the menu data.
    // Vantti has changed their eMenu service and completely removed JSON Data fetching.
    // This implementation relies on HTML Web scraping, which isn't ideal.
    // Hopefully an API is released for this service.
    internal static class Vantti
    {
        // URL for accessing the Vantti website
        private const string VanttiURL = "https://aromimenu.cgisaas.fi/VantaaAromieMenus/FI/Default/Vantti/Sotunkil/Restaurant.aspx";

        // This was retrieved by right clicking on the food text, inspect element, copy xPath.
        // Can you think of a better solution?
        private static string Get_xPath(bool isVegan)
        {
            return $"/html/body/form/div[3]/div[1]/div/div/div/div[2]/div[2]/div[1]/div[5]/div/div/div[{((isVegan)?2:3)}]/div/div[1]/div/div[1]/div[2]";
        }

        // Filter out all unnecessary characters from the node and fetch our text
        private static List<string> GetTextContent(HtmlNode node)
        {
            if ( node == null )
            {
                return null;
            }
            List<string> str = new List<string>();
            foreach (var child in node.ChildNodes)
            {
                string innerText = child.InnerText;
                if (innerText.Count(_ => _ == ' ') < 10 && innerText.Trim(' ').Length > 2)
                {
                    str.Add(innerText);
                }
            }
            return str;
        }

        // Returns today's food as a string in the form of: "Lounas [Food], Kasvislounas [Food] etc."
        public static string GetFood()
        {
            try
            {
                var web = new HtmlWeb(); // Create new HtmlWeb Object
                var doc = web.Load(VanttiURL); // Load the HTML file from Vantti
                var notVegan = GetTextContent(doc.DocumentNode.SelectSingleNode(Get_xPath(false)));
                var vegan = GetTextContent(doc.DocumentNode.SelectSingleNode(Get_xPath(true)));
                if (notVegan == null)
                {
                    return "Ruokatietoja ei saatavilla! :(";
                }
                // Create the final text
                string topText =
                    "Lounas                " + notVegan[0] + " " + notVegan[1] +
                    "\n                           " + notVegan[2] + " " + notVegan[3];
                string bottomText = "";

                if (vegan != null) 
                { 
                    bottomText =
                    "Kasvislounas        " + vegan[0] + " " + vegan[1] +
                    "\n                           " + vegan[2] + " " + vegan[3];
                }

                topText = FixSpecialCharacters(topText);
                bottomText = FixSpecialCharacters(bottomText);

                return $"{topText}\n\n{bottomText}";
            }
            catch (Exception)
            {
                Console.WriteLine("NullReferenceException occurred in retrieving food information.");
            }
            return "Ruokatietoja ei saatavilla.";
        }

        // Legacy Functions, might come in handy?
        private static bool IsWeekend(DayOfWeek date)
        {
            return date == DayOfWeek.Saturday || date == DayOfWeek.Sunday;
        }

        private static string FixSpecialCharacters(string content)
        {
            return content.Replace("&#228", "ä");
        }
    }
}
