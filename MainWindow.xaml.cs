using System;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Net;
using System.IO;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Linq;

namespace Sotunki_TV
{
    public class WeatherList
    {
        public List<WeatherObject> Weather { get; set; }
    }
    public class WeatherObject
    {
        public string main { get; set; }
        public string icon { get; set; }
    }
    public partial class MainWindow : Window
    {

        string[] Picture_Folder_Paths =
        {
            @"G:\Oma Drive\Pictures",
            @"F:\Oma Drive\Pictures",
            @"G:\My Drive\Pictures",
            @"F:\My Drive\Pictures",
            @"E:\"
        };

        private readonly string instagramBaseAPIUrl = "https://graph.instagram.com/";
        string[] InstaParse = { "{\"data\":[{\"caption\":\"", "\",\"media_url\":\"", "\",\"thumbnail_url\":\"", "\",\"id\":" };

        private string Instagram_AccessToken = "null";
        private string newToken = "null";

        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        //If picture has no delay declared, what is the default.
        private const int Default_Slide_Delay = 5;
        //What slide will the info be updated on.
        private const int UpdatePosition = 2;
        //What slide will we update the picture on
        private const int PicturePosition = 3;

        DispatcherTimer Picture_Update = new DispatcherTimer();
        DoubleAnimation myDoubleAnimation = new DoubleAnimation();
        
        //Constant values that specify the x position of the grid that contains all slides.
        //Index number 4 is where we need to go back to if there are several pictures.
        private int[] Screen_Positions = { 0, -810, -1615, -2415, -3222, -4025 };

        private int Current_Picture = 0;
        private int Current_PictureLength = 0;

        //At what screen are we currently on
        int Screen_Progress_Positions = 0;
        //These are handling the Google Drive Picture Delays
        //First keep track of seconds passed
        //Then see when we should change slides
        //And set the next delay
        int Seconds_ToProgress = 4;
        int Seconds_RightNow = 0;
        int Seconds_WillProgress = 0;
        string Seconds_ResetProgress = "";


        public MainWindow()
        {
            InitializeComponent();
            SetEvents();
        }

        //This sets all of the ticking events at the start of the program
        private void SetEvents()
        {
            Picture_Update.Interval = new TimeSpan(0, 0, 0, 1);
            Picture_Update.Tick += this.UpdatePicture;
            Picture_Update.Start();

            UpdateClock();
            SetPassword();
            SetAnimation();
            Update_Info(null,null);
            Loader.Visibility = Visibility.Visible;
        }
        private void SetPassword()
        {
            if(File.Exists(@"C:\Sotunki-TV\account.txt"))
            {
                string text = File.ReadAllText(@"C:\Sotunki-TV\account.txt");
                Instagram_AccessToken = text;
            }
        }

        private void SetInstagram()
        {
            if(Instagram_AccessToken == "null") { return; } 
            var x = RefreshToken(Instagram_AccessToken);
            GetInstagramContents(x);
        }

        public string RefreshToken(string userToken)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    string queryUrl = $"refresh_access_token?grant_type=ig_refresh_token&access_token={userToken}";
                    string fullUrl = $"{this.instagramBaseAPIUrl}{queryUrl}";
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var newresponse = client.GetStringAsync(new Uri(fullUrl)).Result;
                    var data = (JObject)JsonConvert.DeserializeObject(newresponse);
                    string expires = data["expires_in"].Value<string>();
                    newToken = data["access_token"].Value<string>();
                }
                catch (Exception){}
            }
            return newToken;
        }
        void GetInstagramContents(string userToken)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    string queryUrl = $"me/media?fields=caption,thumbnail_url,media_url&limit=1&access_token={userToken}";
                    string fullUrl = $"{this.instagramBaseAPIUrl}{queryUrl}";
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var response = client.GetStringAsync(new Uri(fullUrl)).Result;
                    if (!string.IsNullOrEmpty(response))
                    {
                        int emojiIndex;
                        string removeE;
                        string[] FinnishHex = { "\\u00c4", "\\u00e4", "\\u00f6", "\\u00d6" };
                        string[] Finnishletters = { "Ä", "ä", "ö", "Ö" };
                        var InstaData = response.Split(InstaParse, System.StringSplitOptions.RemoveEmptyEntries);
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
                        InstaMedia_Text.Text = InstaData[0];
                        InstaMedia_Image.Source = new BitmapImage(new Uri(InstaData[1]));
                    }
                }
                catch (Exception){}
            }
        }

        private void SetAnimation()
        {
            QuadraticEase easing = new QuadraticEase();
            easing.EasingMode = EasingMode.EaseInOut;
            myDoubleAnimation.Duration = new Duration(TimeSpan.FromSeconds(2));
            myDoubleAnimation.EasingFunction = easing;
            myDoubleAnimation.Completed += new EventHandler(Picture_Faded);
        }


        private String[] GetFilesFrom(String searchFolder, String[] filters, bool isRecursive)
        {
            List<String> filesFound = new List<String>();
            var searchOption = isRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (var filter in filters)
            {
                filesFound.AddRange(Directory.GetFiles(searchFolder, String.Format("*.{0}", filter), searchOption));
            }
            return filesFound.ToArray();
        }

        private void UpdatePictureSlides()
        {
            String[] files = new string[0];
            String searchFolder;
            //Check for each directory specified in Path Array, and if found. Then get from that folder.
            for (int i = 0; i < Picture_Folder_Paths.Length; i++)
            {
                if (Directory.Exists(Picture_Folder_Paths[i]))
                {
                    searchFolder = Picture_Folder_Paths[i];
                    var filters = new String[] { "jpg", "jpeg", "png", "gif", "tiff", "bmp", "svg" };
                    files = GetFilesFrom(searchFolder, filters, false);
                    Current_PictureLength = files.Length;
                    Array.Sort(files);
                    break;
                }
            }
            string ChosenPicture;
            string SecondChosenPicture;
            Uri PathToSecond;
            bool UsingDefault;
            //If no pictures are in the folder. Use the default school picture.
            if (files.Length < 1)
            {
                Random rand = new Random();
                ChosenPicture = @"/Resources/IdlePics/1_1 (" + rand.Next(1,39) +").jpg";
                Uri Path_ToImage1 = new Uri(ChosenPicture, UriKind.Relative);
                Picture_Second.Source = new BitmapImage(Path_ToImage1);
                UsingDefault = true;
            }
            else if(files.Length > 1 && Current_Picture + 1 < files.Length)
            {
                ChosenPicture = files[Current_Picture];
                SecondChosenPicture = files[Current_Picture + 1];
                PathToSecond = new Uri(SecondChosenPicture);
                Picture_Main_Second.Source = new BitmapImage(PathToSecond);
                Picture_Second.Source = new BitmapImage(PathToSecond);
                Seconds_ResetProgress = SecondChosenPicture;
                UsingDefault = false;
            }
            else
            {
                ChosenPicture = files[Current_Picture];
                Seconds_ResetProgress = ChosenPicture;
                UsingDefault = false;
            }

            Uri Path_ToImage = (UsingDefault) ?
                new Uri(ChosenPicture, UriKind.Relative) :
                new Uri(ChosenPicture);
            //Set All Picture Components
            Picture_Main.Source = new BitmapImage(Path_ToImage);
            if(files.Length == 1) { Picture_Second.Source = new BitmapImage(Path_ToImage); }

            //Define the delay for slide
            Seconds_WillProgress = Progression_Calculator(ChosenPicture);
            Current_Picture++;
        }

        int Progression_Calculator(string Name)
        {
            string FileName = Path.GetFileName(Name);
            string Delay_Number = "8";
            try { Delay_Number = FileName.Split('_')[1].Split('.')[0]; }
            catch (IndexOutOfRangeException) { }
            bool success = Int32.TryParse(Delay_Number, out int New_Delay);
            return (!success) ? Default_Slide_Delay : New_Delay;
        }

        //This runs after the animation of sliding has finished.
        private void Picture_Faded(object sender, EventArgs e)
        {
            int SPP = Screen_Progress_Positions;
            Seconds_ToProgress = Default_Slide_Delay;
            if(SPP == PicturePosition) { UpdatePictureSlides(); Current_Picture = 0; }
            if(SPP == UpdatePosition) { Update_Info(null,null); }
            else if(SPP > PicturePosition) {
                if(Current_Picture >= Current_PictureLength-1) {
                    Move_Screen.RenderTransform = new TranslateTransform(0, 0);
                    Seconds_WillProgress = Progression_Calculator(Seconds_ResetProgress);
                    Screen_Progress_Positions = 0;
                    Current_Picture = 0;
                }
                else {
                    UpdatePictureSlides();
                    Screen_Progress_Positions = PicturePosition + 1;
                    Move_Screen.RenderTransform = new TranslateTransform(Screen_Positions[Screen_Progress_Positions], 0);
                }
                Seconds_ToProgress = Seconds_WillProgress;
            }
        }

        //This runs every second due to a timer.
        private void UpdatePicture(object sender, EventArgs e)
        {
            UpdateClock();
            Seconds_RightNow++;
            if(Seconds_RightNow < Seconds_ToProgress + 2 || Screen_Progress_Positions >= Screen_Positions.Length-1){return;}
            Seconds_RightNow = 0;
            myDoubleAnimation.From = Screen_Positions[Screen_Progress_Positions];
            myDoubleAnimation.To = Screen_Positions[Screen_Progress_Positions+1];
            Screen_Progress_Positions++;
            Loader.RenderTransform.BeginAnimation(RotateTransform.AngleProperty, null);
            Move_Screen.RenderTransform.BeginAnimation(TranslateTransform.XProperty, myDoubleAnimation);
            if (IsConnectedToInternet()) { Loader.Visibility = Visibility.Hidden; }
        }

        //This is a horrible implementation, but it works
        private string FindSotunkiJSON(string raw)
        {
            string thisDate = DateTime.Today.Date.AddDays(0).ToString("d.M.yyyy");
            string[] rawSplit = raw.Split(',');
            List<string> PossibleUrls = new List<string>();
            bool schoolFound = false;
            for(int i = 0; i < rawSplit.Length; i++)
            {
                if(schoolFound)
                {
                    if(rawSplit[i].Contains("Name")) { break; }
                    else if (rawSplit[i].Contains("LinkUrl")) 
                    { 
                        string formatLink = rawSplit[i].Substring(11, rawSplit[i].Length - 11).Replace("\"", "").Replace("}", "");
                        PossibleUrls.Add(formatLink);
                    }
                }
                if(rawSplit[i].Contains("Sotungin koulu")) 
                {
                    schoolFound = true;
                }
            }
            if(schoolFound)
            {
                for (int i = PossibleUrls.Count - 1; i >= 0; i--)
                {
                    string RawDayJson = new WebClient().DownloadString("https:" + PossibleUrls[i]);
                    if (RawDayJson.Contains(thisDate)) { return RawDayJson; }
                }
            }
            return null;
        }
        //I don't even know, but maybe it will work for a while
        private string FindSotunkiFood(string raw, string day, bool isVegan)
        {
            string[] rawSplit = raw.Split(',');
            bool dayFound = false;
            bool veganFound = false;
            int correctIndex = 0;
            for (int i = 0; i < rawSplit.Length; i++)
            {
                if (dayFound)
                {
                    if (rawSplit[i].Contains("Name") && !isVegan) { correctIndex = i; break; }
                    else if (rawSplit[i].Contains("Name") && veganFound && isVegan) { correctIndex = i; break; }
                }
                if (rawSplit[i].Contains(day))
                {
                    dayFound = true;
                }
                if (rawSplit[i].Contains("Kasvislounas") && dayFound)
                {
                    veganFound = true;
                }
            }
            if(!dayFound) { return null; }
            string UnFormattedFood = rawSplit[correctIndex];
            string FormattedFood = UnFormattedFood.Substring(8, UnFormattedFood.Length - 8);
            if(!rawSplit[correctIndex+1].Contains("Price")) 
            {
                string addition = rawSplit[correctIndex + 1].Remove(0, 1);
                FormattedFood += '\n' + "                           " + addition; 
                if (!rawSplit[correctIndex + 2].Contains("Price")) { FormattedFood += rawSplit[correctIndex + 2]; }
            }
            FormattedFood = FormattedFood.Replace("\"", "");
            return FormattedFood;
        }

        private bool IsWeekend(DayOfWeek date)
        {
            return date == DayOfWeek.Saturday || date == DayOfWeek.Sunday;
        }

        private string fixSpecialCharacters(string content)
        {
            return content.Replace("Ã¶", "ö").Replace("Ã¤", "ä");
        }

        private void NoFood()
        {
           Food_Label.Content = "Ruokatieto ei saatavilla. :(\nTule takaisin myöhemmin.";
           Second_Food_Label.Content = "";
        }
        private void SetFoodLabel()
        {
            try
            {
                string RawJsonData = new WebClient().DownloadString("https://ruokalistat.vantti.fi/AromiStorageVantti/blob/main/AromiMenusJsonData");
                if (RawJsonData == null) { NoFood(); return; }
                string RawDayJson = FindSotunkiJSON(RawJsonData);
                if(RawDayJson == null) { NoFood(); return; }
                //First Food
                DateTime currentDay = DateTime.Today;

                currentDay = currentDay.AddDays(0);

                DayOfWeek weekDay = currentDay.DayOfWeek;
                string MainMeal = FindSotunkiFood(RawDayJson, weekDay.ToString(), false);
                string VeganMeal = FindSotunkiFood(RawDayJson, weekDay.ToString(), true);
                if(IsWeekend(weekDay))
                {
                    Food_Label.Content = "Hyvää Viikonloppua!";
                    Second_Food_Label.Content = "";
                    return;
                }
                if(MainMeal == null && VeganMeal == null) 
                {
                    NoFood(); return;
                }
                MainMeal = fixSpecialCharacters(MainMeal);
                VeganMeal = fixSpecialCharacters(VeganMeal);
                if (MainMeal == VeganMeal)
                {
                    Food_Label.Content = $"Lounas                {MainMeal}";
                }
                else
                {
                    Food_Label.Content = $"Lounas                {MainMeal}\n\nKasvislounas        {VeganMeal}";
                }
                //Second Food
                DayOfWeek tomorrowDay = currentDay.AddDays(1).DayOfWeek;
                if(IsWeekend(tomorrowDay)) { Second_Food_Label.Content = "Hyvää Viikonloppua :D"; return;}
                MainMeal = FindSotunkiFood(RawDayJson, tomorrowDay.ToString(), false);
                VeganMeal = FindSotunkiFood(RawDayJson, tomorrowDay.ToString(), true);
                if (MainMeal == null && VeganMeal == null)
                {
                    Second_Food_Label.Content = ""; return;
                }
                MainMeal = fixSpecialCharacters(MainMeal);
                VeganMeal = fixSpecialCharacters(VeganMeal);
                Second_Food_Label.Content = $"Huominen lounas\n{MainMeal.Replace("                           ","")}";
                return;
                /*
                string[] days = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };
                string food;
                int a = Array.IndexOf(days, currentDay) + 1;
                string Final = "Tänään on ruokana : \n";
                for (int i = 1; i < 4; i++)
                {
                    food = "/html/body/main/article/section[1]/div/div[1]/div[2]/div[2]/div/div/div[" + (int)a + "]/ul/li[" + i + "]/p[1]/span[2]";
                    string printFood;
                    
                    if (docus == null) { continue; }
                    printFood = docus.InnerText;
                    if(printFood.Length > 40) { printFood = printFood.Substring(0,40) + "..."; }
                    Final += printFood + "\n";
                }
                if (Final.Length < 30) {
                    Second_Food_Label.Content = " ";
                    Food_Label.Content = "Ruokatieto ei saatavilla. :(\nTule takaisin myöhemmin."; return; 
                }
                Food_Label.Content = Final;
                if(a > 4) { Second_Food_Label.Content = "Hyvää viikonloppua!"; return; }
                Final = "Huomenna on ruokana : \n";
                //Lets get the tomorrows food
                for (int i = 1; i < 3; i++)
                {
                    food = "/html/body/main/article/section[1]/div/div[1]/div[2]/div[2]/div/div/div[" + (int)(a+1) + "]/ul/li[" + i + "]/p[1]/span[2]";
                    string printFood;
                    var docus = doc.DocumentNode.SelectSingleNode(food);
                    if (docus == null) { continue; }
                    printFood = docus.InnerText;
                    if (printFood.Length > 30) { printFood = printFood.Substring(0, 28) + "..."; }
                    Final += printFood + "\n";
                }
                if (Final.Length < 30) { Final = " "; }
                Second_Food_Label.Content = Final;*/
            }
            catch(WebException) { return; };  
        }
        private void UpdateClock()
        {
            Label_Time.Content = DateTime.Now.ToString("HH:mm");
        }
        private void SetBusTimes()
        {

            var currentTime = DateTime.Now;
            int secondsNow = currentTime.Hour * 60 * 60 + currentTime.Minute * 60 + currentTime.Second;
            string responseFromServer = " ";
            string[] Bus_Names;
            string[] separated = { "{\"data\":{\"stops\":[{\"stoptimesWithoutPatterns\":[{\"realtimeArrival\":", ",\"trip\":{\"route\":{\"shortName\":\"", "\"}}},{\"realtimeArrival\":", "\"}}}]}]}}" };
            void send(string postData)
            {
                WebRequest request;
                Stream dataStream;
                WebResponse response;
                try
                {
                    request = WebRequest.Create("https://api.digitransit.fi/routing/v1/routers/hsl/index/graphql");
                    request.Method = "POST";
                    byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                    request.ContentType = "application/graphql";
                    request.ContentLength = byteArray.Length;
                    dataStream = request.GetRequestStream();
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Close();
                    response = request.GetResponse();
                }
                catch(System.Net.WebException) { return; }
                
                using (dataStream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(dataStream);
                    responseFromServer = reader.ReadToEnd();
                    reader.Close();
                }
                dataStream.Close();
                response.Close();
                request.Method = "DELETE";

            }
            send("{stops(name: \"V9424\") {stoptimesWithoutPatterns {realtimeArrival trip { route { shortName } } } } }");
            List<string> times;
            string[] final;
            void updateValues()
            {
                //Here we declare the times of buses
                int[] list = new int[5];
                times = new List<string>();
                for (int a = 0; a < 9; a += 2)
                {
                    if (a < Bus_Names.Length)
                    {
                        list[a / 2] = Int32.Parse(Bus_Names[a]);
                        list[a / 2] = (list[a / 2] - secondsNow) / 60;
                    }
                    else
                    {
                        list[a / 2] = 0;
                    }
                    if (a + 1 < Bus_Names.Length)
                    {
                        times.Add(Bus_Names[a + 1]);
                    }
                    else
                    {
                        times.Add("");
                    }
                }

                int[] mins = { 0, 1, 2, 3, 4 };
                int[] hrs = { 0, 1, 2, 3, 4 };
                final = new string[] { " ", " ", " ", " ", " " };
                bool[] isHour = new bool[5];

                for (int g = 0; g < 5; g++)
                {
                    mins[g] = list[g] % 60;
                    hrs[g] = list[g] / 60;
                    if (hrs[g] == 0)
                    {
                        isHour[g] = true;
                    }
                    if (isHour[g])
                    {
                        final[g] = "" + mins[g] + " min";
                    }
                    else
                    {
                        final[g] = "" + hrs[g] + " h " + mins[g] + " min";
                    }

                    if((hrs[g] < 0 || mins[g] < 0))
                    {
                        final[g] = "";
                        times[g] = "";
                    }

                    if(hrs[g] == 0 && mins[g] == 0)
                    {
                        final[g] = "Nyt";
                    }
                }
            }
            string[] FullList = new string[4];

            string[] Send_Functions =
            {
                "{stops(name: \"V9424\") {stoptimesWithoutPatterns {realtimeArrival trip { route { shortName } } } } }",
                "{stops(name: \"V9425\") {stoptimesWithoutPatterns {realtimeArrival trip { route { shortName } } } } }",
                "{stops(name: \"V9402\") {stoptimesWithoutPatterns {realtimeArrival trip { route { shortName } } } } }",
                "{stops(name: \"V9403\") {stoptimesWithoutPatterns {realtimeArrival trip { route { shortName } } } } }"
            };

            string[] Stop_Names =
            {
                "(V9424)",
                "(V9425)",
                "(V9402)",
                "(V9403)"
            };

            //Set Text Numbers Bus Stops 1,2,3,4
            string[] Bus_Times_Text = new string[4];
            if(responseFromServer.Length < 2) { return; }
            for(int i = 0; i < 4; i++)
            {
                send(Send_Functions[i]);
                Bus_Names = responseFromServer.Split(separated, StringSplitOptions.RemoveEmptyEntries);
                updateValues();
                //FullList[i] += "Pysäkki: " + Stop_Names[i] + '\n';
                if(i==1||i==3)
                {
                    FullList[i] += '\n';
                    Bus_Times_Text[i] += '\n';
                    FullList[i] += '\n';
                    Bus_Times_Text[i] += '\n';
                }

                for(int d = 0; d < 5; d++)
                {
                    Bus_Times_Text[i] +=
                        final[d] + '\n';
                    FullList[i] +=
                        times[d] + '\n';
                }   
            }
            //Bus names are the numbers 587 or 717A , meaning the names
            //Bus Times are when they will arrive.
            Bus_Names_1.Content = FullList[0] + FullList[1];
            Bus_Times_1.Content = Bus_Times_Text[0] + Bus_Times_Text[1];
            Bus_Names_2.Content = FullList[2] + FullList[3];
            Bus_Times_2.Content = Bus_Times_Text[2] + Bus_Times_Text[3];
        }

        private void SetWeather()
        {
            try
            {
                WebClient client = new WebClient();
                string downloadString = client.DownloadString("http://api.openweathermap.org/data/2.5/weather?lat=60.28&lon=25.12&appid=8e5221c6c28240c09c408389a9c6bc04");

                var result = JsonConvert.DeserializeObject<WeatherList>(downloadString);
                var data = JObject.Parse(downloadString);
                string tempstring = Convert.ToString(data["main"]["temp"]);
                var windspeed = Convert.ToString(data["wind"]["speed"]);
                var weatherdescription = result.Weather.Select(p => p.main).ToArray();
                var weathericon = result.Weather.Select(p => p.icon).ToArray();
                float temp = float.Parse(tempstring);
                temp -= 273.15f;
                temp = (int)temp;

                var Date_t = DateTime.Now;
                var Date_s = Date_t.Date.ToString("d/M/yyyy");
                Weather_Label.Content =
                "Vantaa " + Date_s + "\n" +
                weatherdescription[0] + " " + temp + " ° C";
                string bitmapPath = @"/Resources/" + weathericon[0] + ".png";
                BitmapImage bitmapImage = new BitmapImage(new Uri(bitmapPath, UriKind.Relative));
                Weather_Icon.Source = bitmapImage;
            }
            catch (Exception) { Console.WriteLine("fail"); }
        }

        private void MoveCursor()
        {
            int x = new Random().Next(0,100);
            int y = new Random().Next(0,100);
            SetCursorPos(x, y);
        }

        [DllImport("wininet.dll")]
        private static extern bool InternetGetConnectedState(out int Description, int ReservedValue);

        //Creating a function that uses the API function...
        public static bool IsConnectedToInternet()
        {
            return InternetGetConnectedState(out _, 0);
        }

        //This function is called whenever screen position is at the UpdatePosition. Refer to the variable
        private void Update_Info(object sender, EventArgs e)
        {
            if(!IsConnectedToInternet()) { Loader.Visibility = Visibility.Visible; return; } //Checking if we have internet.
            else { Loader.Visibility = Visibility.Hidden; }
            SetFoodLabel();
            SetBusTimes();
            SetWeather();
            SetInstagram();
            MoveCursor();

        }
    }
}
