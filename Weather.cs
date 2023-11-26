using System.Windows.Media.Imaging;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Linq;
using System.Net;
using System;

namespace Sotunki_TV
{
    // This class uses the openweathermap API for retrieving local weather data.
    // The API does not require any key as of yet.
    internal static class Weather
    {
        private const string OpenWeatherMapAPI_URL = "http://api.openweathermap.org/data/2.5/weather?lat=60.28&lon=25.12&appid=8e5221c6c28240c09c408389a9c6bc04";
        private class WeatherList
        {
            public List<WeatherObject> Weather { get; set; }
        }
        private class WeatherObject
        {
            public string Main { get; set; }
            public string Icon { get; set; }
        }

        static public Tuple<string, BitmapImage> GetWeather()
        {
            try
            {
                // Get response from OpenWeatherMap
                WebClient client = new WebClient();
                string downloadString = client.DownloadString(OpenWeatherMapAPI_URL);

                // Deserialize, parse and convert to string
                var result = JsonConvert.DeserializeObject<WeatherList>(downloadString);
                var data = JObject.Parse(downloadString);
                string tempstring = Convert.ToString(data["main"]["temp"]);
                var windspeed = Convert.ToString(data["wind"]["speed"]);

                var weatherdescription = result.Weather.Select(p => p.Main).ToArray();
                var weathericon = result.Weather.Select(p => p.Icon).ToArray();

                float temp = float.Parse(tempstring);
                temp -= 273.15f; // Temperature is in Kelvin, we want it in Celsius
                temp = (int)temp; // Convert to Int

                var Date_s = DateTime.Now.Date.ToString("d/M/yyyy");

                // Create the text
                string weatherDescription =
                "Vantaa " + Date_s + "\n" +
                weatherdescription[0] + " " + temp + " ° C";

                // Get the correct image from folder
                string bitmapPath = @"/Resources/weatherIcons/" + weathericon[0] + ".png";
                BitmapImage weatherImage = new BitmapImage(new Uri(bitmapPath, UriKind.Relative));

                return new Tuple<string, BitmapImage>(weatherDescription, weatherImage);
            }
            catch (Exception) 
            { 
                Console.WriteLine("Exception in retrieving the weather."); 
            }
            return null;
        }
    }
}
