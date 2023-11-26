using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System;

namespace Sotunki_TV
{
    // This class retrieves HSL data by using the DigiTransit API
    // Refer to the API Docs to how or why the data is handled the way it is.
    // The usage of this API requires a key. Refer to this guide: https://digitransit.fi/en/developers/api-registration/
    internal class HSL
    {
        private string Digitransit_AccessToken = "null";

        // The string used for sending the web request to DigiTransit API
        private readonly string[] requestFunctions =
        {
            "{stops(name: \"V9424\") {stoptimesWithoutPatterns {realtimeArrival trip { route { shortName } } } } }",
            "{stops(name: \"V9425\") {stoptimesWithoutPatterns {realtimeArrival trip { route { shortName } } } } }",
            "{stops(name: \"V9402\") {stoptimesWithoutPatterns {realtimeArrival trip { route { shortName } } } } }",
            "{stops(name: \"V9403\") {stoptimesWithoutPatterns {realtimeArrival trip { route { shortName } } } } }"
        };

        // This is used to filter the web result 
        private readonly string[] requestFilter = { 
            "{\"data\":{\"stops\":[{\"stoptimesWithoutPatterns\":[{\"realtimeArrival\":", 
            ",\"trip\":{\"route\":{\"shortName\":\"", 
            "\"}}},{\"realtimeArrival\":", 
            "\"}}}]}]}}" 
        };

        // Gets the token from a file and sets the token variable
        public void GetToken()
        {
            if (File.Exists(@"C:\Sotunki-TV\digitransit.txt"))
            {
                Digitransit_AccessToken = File.ReadAllText(@"C:\Sotunki-TV\digitransit.txt");
            }
        }

        // Sends a web request to the API and returns the resulting string
        private string SendRequest(string postData)
        {
            WebRequest request;
            Stream dataStream;
            WebResponse response;
            string responseStr;
            try
            {
                request = WebRequest.Create("https://api.digitransit.fi/routing/v1/routers/hsl/index/graphql");
                request.Headers["digitransit-subscription-key"] = Digitransit_AccessToken;
                request.Method = "POST";
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                request.ContentType = "application/graphql";
                request.ContentLength = byteArray.Length;
                dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                response = request.GetResponse();
            }
            catch (WebException) { return null; }

            using (dataStream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(dataStream);
                responseStr = reader.ReadToEnd();
                reader.Close();
            }
            dataStream.Close();
            response.Close();
            request.Method = "DELETE";
            return responseStr;
        }

        // Returns current time of day in seconds.
        private int CurrentTimeInSeconds()
        {
            var currentTime = DateTime.Now;
            return currentTime.Hour * 60 * 60 + currentTime.Minute * 60 + currentTime.Second;
        }

        private Tuple<string, string>[] ConvertRequest(string[] filteredRequest)
        {
            int secondsNow = CurrentTimeInSeconds();

            List<string> busIdentifiers = new List<string>(); // Identifiers of the buses
            List<string> busArrivalTimes = new List<string>(); // Arrival time in descriptive words example: 1 hr 30 min
            List<int> arrivalTimes = new List<int>(); // Arrival time in minutes

            for (int i = 0; i < filteredRequest.Length; i++)
            {
                if (i % 2 == 0) // Arrival time of bus
                {
                    // This should be a number, but just to be safe.
                    int arrivalTime = int.TryParse(filteredRequest[i], out arrivalTime) ? arrivalTime : 0;
                    arrivalTime %= 86400; // Refer to the API Docs why the returned value could be more than 86400
                    arrivalTime = Math.Abs(arrivalTime - secondsNow) / 60; // Convert seconds to minutes
                    arrivalTimes.Add(arrivalTime); // Add the minutes to list
                }
                else // Identifier of bus
                {
                    busIdentifiers.Add(filteredRequest[i]); // Add Identifier
                }
            }

            // Convert minutes into descriptions, example: 1 hr 30 min

            foreach(int time in arrivalTimes)
            {
                int hours = (int)Math.Floor((double)time / 60.0); // Convert minutes to hours
                int minutes = time - hours * 60; // Get leftover minutes

                string hourDescription = (hours > 0) ? $"{hours} hr" : "";
                string minuteDescription = (minutes > 0) ? $"{minutes} min" : "";
                string busArrivalTime = (hours > 0) ? $"{hourDescription} {minuteDescription}" : minuteDescription;

                if (hours == 0 && minutes < 2) { busArrivalTime = "Nyt"; } // Bus is coming now!
                if (time < 0) { busArrivalTime = ""; }

                busArrivalTimes.Add(busArrivalTime);
            }

            // Make an array of Tuple(name, arrivalTime) for example: 717N, 1hr 30 min  

            Tuple<string, string>[] result = new Tuple<string, string>[busIdentifiers.Count];

            for(int i = 0; i < busIdentifiers.Count && i < busArrivalTimes.Count; i++)
            {
                result[i] = new Tuple<string, string>(busIdentifiers[i], busArrivalTimes[i]);
            }

            return result;
        }

        // Returns an array consisting of a string of bus names and bus arrival times in turns.
        // 0th element is names, 1st element is times, 2nd element is names and 3rd element is times.
        public string[] GetBusSchedule()
        {
            if (Digitransit_AccessToken == "null")
            {
                return null;
            }

            int numOfStops = requestFunctions.Length; // The amount of bus stops
            string[] busIdentifiers = new string[numOfStops]; // 717K, 587 etc.
            string[] busArrivalTimes = new string[numOfStops]; // Arrival times

            for (int i = 0; i < numOfStops; i++)
            {
                // Get API Data
                string response = SendRequest(requestFunctions[i]);
                string[] filteredRequest = response.Split(requestFilter, StringSplitOptions.RemoveEmptyEntries);

                // Convert that data into simple strings.
                Tuple<string, string>[] convertedRequest = ConvertRequest(filteredRequest);

                // Put the data into our two arrays.
                foreach (var pair in convertedRequest)
                {
                    busIdentifiers[i] += $"{pair.Item1}\n";
                    busArrivalTimes[i] += $"{pair.Item2}\n";
                }
            }

            // Define the resulting strings.

            string[] result = new string[4];

            result[0] = busIdentifiers[0] + "\n\n" + busIdentifiers[1];
            result[1] = busArrivalTimes[0] + "\n\n" + busArrivalTimes[1];
            result[2] = busIdentifiers[2] + "\n\n" + busIdentifiers[3];
            result[3] = busArrivalTimes[2] + "\n\n" + busArrivalTimes[3];

            return result;
        }
    }
}
