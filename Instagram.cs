using System.Windows.Media.Imaging;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using System.IO;
using System;

// This class handles the retrieval of the latest Instagram post from the user account.
// NOTE: You are required to have a text file called "account" in C:/Sotunki-TV, the file should have your
// Instagram API access token in it. Refer to this guide: https://docs.oceanwp.org/article/487-how-to-get-instagram-access-token

namespace Sotunki_TV
{
    internal class Instagram
    {

        // The base url for using the Instagram API
        private readonly string instagramBaseAPIUrl = "https://graph.instagram.com/";

        // Parsing the instagram response data.
        private string[] InstaParse = { "{\"data\":[{\"caption\":\"", "\",\"media_url\":\"", "\",\"thumbnail_url\":\"", "\",\"id\":" };

        private string Instagram_AccessToken = "null"; // Your personal access token

        private string newToken = "null"; // Refreshed token, refer to RefreshToken

        // Retrieves and sets the access token
        private void RetrieveAccessToken()
        {
            if (File.Exists(@"C:\Sotunki-TV\account.txt"))
            {
                Instagram_AccessToken = File.ReadAllText(@"C:\Sotunki-TV\account.txt");
            }
        }

        private string RefreshToken(string userToken)
        {
            // Access token not configured
            if (userToken == "null") 
            { 
                return "null";
            }
            using (var client = new HttpClient())
            {
                try // There could be an exception, if request fails.
                {
                    // Create the request url
                    string queryUrl = $"refresh_access_token?grant_type=ig_refresh_token&access_token={userToken}";
                    string fullUrl = $"{this.instagramBaseAPIUrl}{queryUrl}";

                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var newresponse = client.GetStringAsync(new Uri(fullUrl)).Result; // Result from API

                    // Get newToken
                    var data = (JObject)JsonConvert.DeserializeObject(newresponse);
                    string expires = data["expires_in"].Value<string>();
                    newToken = data["access_token"].Value<string>();
                }
                catch (Exception) { Console.WriteLine("Exception during RefreshToken"); } // Catch Exception
            }
            return newToken;
        }

        // Fetches the latest post's description and image in the form of a BitmapImage
        public Tuple<string, BitmapImage> FetchLatestInstagramPost()
        {
            newToken = RefreshToken(Instagram_AccessToken);
            if (newToken == "null")
            { 
                return null; 
            }
            using (var client = new HttpClient())
            {
                try
                {
                    // Create the API URL
                    string queryUrl = $"me/media?fields=caption,thumbnail_url,media_url&limit=1&access_token={newToken}";
                    string fullUrl = $"{this.instagramBaseAPIUrl}{queryUrl}";

                    // Get Response
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var response = client.GetStringAsync(new Uri(fullUrl)).Result;

                    // check if our response is empty or not.
                    if (!string.IsNullOrEmpty(response))
                    {
                        // The description might include emojis or otherwise unwanted characters that we need to filter out.
                        int emojiIndex;
                        string removeE;
                        string[] FinnishHex = { "\\u00c4", "\\u00e4", "\\u00f6", "\\u00d6" };
                        string[] Finnishletters = { "Ä", "ä", "ö", "Ö" };
                        var InstaData = response.Split(InstaParse, StringSplitOptions.RemoveEmptyEntries);
                        for (int letterint = 0; letterint < 4; letterint++)
                        {
                            if (InstaData[0].Contains(FinnishHex[letterint]) == true)
                            {
                                InstaData[0] = InstaData[0].Replace(FinnishHex[letterint], Finnishletters[letterint]);
                            }
                        }
                        while (InstaData[0].Contains("\\u") == true)
                        {
                            if (InstaData[0].Contains("\\u") == false)
                            {
                                break;
                            }
                            emojiIndex = InstaData[0].IndexOf("\\u");
                            removeE = InstaData[0].Substring(emojiIndex, 6);
                            InstaData[0] = InstaData[0].Replace(removeE, string.Empty);
                        }
                        InstaData[1] = InstaData[1].Replace(@"\", string.Empty);

                        // Return our result in the form of a tuple.
                        return new Tuple<string, BitmapImage>(InstaData[0], new BitmapImage(new Uri(InstaData[1])));
                    }
                }
                catch (Exception) { Console.WriteLine("Exception thrown in Instagram image retrieval."); }
            }
            return null; // No response
        }

        // Reads the text file for an access token.
        public void InitInstagram()
        {
            RetrieveAccessToken();
        }
    }
}
