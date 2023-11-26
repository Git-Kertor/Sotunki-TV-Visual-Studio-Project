using System.Runtime.InteropServices;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows;
using System;

namespace Sotunki_TV
{
    // This is the main class, which controls the Front-End elements.
    // Refer to the other classes to how the data is retrieved.<
    public partial class MainWindow : Window
    {

        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        private DispatcherTimer GlobalTimer = new DispatcherTimer();
        private DoubleAnimation slidingAnimation = new DoubleAnimation();

        private Instagram instagram;
        private Images images;
        private HSL hsl;

        public MainWindow()
        {
            InitializeComponent();
            InitProgram();
        }

        // Initializes all necessary classes and starts ticking events.
        private void InitProgram()
        {
            // Init our instagram class
            instagram = new Instagram();
            instagram.InitInstagram();

            // Init our images class
            images = new Images();
            images.RefreshInWindow += new Images.ImageEventHandler(this.RefreshImageComponents);

            // Init our timer that ticks every second
            GlobalTimer.Interval = new TimeSpan(0, 0, 0, 1);
            GlobalTimer.Tick += this.Update;
            GlobalTimer.Start();

            hsl = new HSL();
            hsl.GetToken();

            SetAnimation();
            RefreshComponentInformation(this, null);
            Loader.Visibility = Visibility.Visible;
        }

        private void RefreshImageComponents(object sender, ImageEventArgs e)
        {
            switch (e.EventType)
            {
                case RefreshType.Component:
                    RefreshComponentInformation(this, new EventArgs());
                    break;
                case RefreshType.RectTransform:
                    Move_Screen.RenderTransform = images.rectTransform;
                    break;
                case RefreshType.Image:
                    FirstCustomImage.Source = images.FirstImage;
                    SecondCustomImage.Source = images.SecondImage;
                    StartPicture.Source = images.SecondImage;
                    break;
            }
        }

        // Refresh the text / media in all necessary components.
        private void RefreshComponentInformation(object sender, EventArgs e)
        {
            //Checking if we have internet.
            if (!IsConnectedToInternet()) { Loader.Visibility = Visibility.Visible; return;}
            else { Loader.Visibility = Visibility.Hidden; }

            // We fetch the data from our instagram class.
            var instaData = instagram.FetchLatestInstagramPost();
            if (instaData != null)
            {
                InstaMedia_Text.Text = instaData.Item1;
                InstaMedia_Image.Source = instaData.Item2;
            }

            // We fetch weather data from our weather class.
            var weather = Weather.GetWeather();
            if (weather != null)
            {
                Weather_Label.Content = weather.Item1;
                Weather_Icon.Source = weather.Item2;
            }

            // Set food content
            Food_Label.Content = Vantti.GetFood();

            // Set bus content
            string[] busSchedule = hsl.GetBusSchedule();
            if (busSchedule != null)
            {
                Bus_Names_1.Content = busSchedule[0];
                Bus_Times_1.Content = busSchedule[1];
                Bus_Names_2.Content = busSchedule[2];
                Bus_Times_2.Content = busSchedule[3];
            }

            MoveCursor();
        }

        // Runs every second.
        // This function waits for the sliding animation to finish by checking elapsedSeconds and then updating the animation
        private void Update(object sender, EventArgs e)
        {
            UpdateClock();

            images.elapsedSeconds += 1; // One second has elapsed
            if (images.elapsedSeconds < images.secondsToProgress + 2 || images.currentSlidePosition >= images.Screen_Positions.Length - 1) {
                return; 
            }
            images.elapsedSeconds = 0; // Animation is restarting

            // Change the position and destination of sliding animation.
            slidingAnimation.From = images.Screen_Positions[images.currentSlidePosition];
            slidingAnimation.To = images.Screen_Positions[images.currentSlidePosition + 1];
            images.currentSlidePosition += 1; // Increment the slide position by one

            // Begin new animation.
            Loader.RenderTransform.BeginAnimation(RotateTransform.AngleProperty, null);
            Move_Screen.RenderTransform.BeginAnimation(TranslateTransform.XProperty, slidingAnimation);

            // Check if still connected to internet.
            if (IsConnectedToInternet()) { Loader.Visibility = Visibility.Hidden; }
        }

        // Set our double animation properties. This animation is used in the sliding from image to image.
        private void SetAnimation()
        {
            QuadraticEase easing = new QuadraticEase
            {
                EasingMode = EasingMode.EaseInOut
            };
            slidingAnimation.Duration = new Duration(TimeSpan.FromSeconds(2));
            slidingAnimation.EasingFunction = easing;
            slidingAnimation.Completed += new EventHandler(images.SlidingCompleted);
        }

        // Updates the clock label
        private void UpdateClock()
        {
            Label_Time.Content = DateTime.Now.ToString("HH:mm");
        }

        // Moving the cursor so the computer won't go into screensaver.
        // Is there a better solution?
        private void MoveCursor()
        {
            int x = new Random().Next(0, 100);
            int y = new Random().Next(0, 100);
            SetCursorPos(x, y);
        }

        [DllImport("wininet.dll")]
        private static extern bool InternetGetConnectedState(out int Description, int ReservedValue);

        // Checking if the computer thinks it is currently connected to the internet.
        public static bool IsConnectedToInternet()
        {
            return InternetGetConnectedState(out _, 0);
        }
    }
}
