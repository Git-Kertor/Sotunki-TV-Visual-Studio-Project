using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Windows.Media;
using System.IO;
using System;

namespace Sotunki_TV {

    // This class handles the custom image loading and the timing of the sliding animation.

    internal class ImageEventArgs
    {
        public ImageEventArgs(RefreshType type) { EventType = type; }
        public RefreshType EventType;
    }

    // The type of fired event
    // Refer to MainWindow for how this is utilized
    internal enum RefreshType
    {
        Component, // Update the components
        RectTransform, // Update the rectTransform
        Image // Update the images
    }
    
    internal class Images
    {

        // These values get changed during runtime. 

        // Constant values that specify the x position of the grid that contains all slides.
        // Index number 4 is where we need to go back to if there are several pictures.
        // Going back to index 4 creates the illusion that there are more than two image controls.
        public int[] Screen_Positions = { 0, -810, -1615, -2415, -3222, -4025 };

        public int elapsedSeconds = 0; // How many seconds since we started this animation.
        public int secondsToProgress = 5; // How much does elapsedSeconds have to be until new animation.
        public int futureSecondsToProgress = 0; // Store the future value of secondsToProgress. 
        public int currentSlidePosition = 0; // The index of the currently visible slide.

        // Main Window will access these to update its own objects.
        public TranslateTransform rectTransform;
        public BitmapImage FirstImage;
        public BitmapImage SecondImage;

        // When we select two custom images, store the second one's name here.
        private string futureImageToProcess = "null";

        // Default delay between slides in seconds
        private const int Default_Slide_Delay = 5;
        //What slide will the info be updated on.
        private const int UpdatePosition = 2;
        //What slide will we update the picture on
        private const int PicturePosition = 3;

        private int currentPicture = 0; // The index of our currently viewed image.
        private int imageFolderSize = 0; // Number of images stored in the image folder.

        // The directories of where our custom images are stored.
        private string[] ImageFolderDirectories =
        {
            @"G:\Oma Drive\Pictures",
            @"F:\Oma Drive\Pictures",
            @"G:\My Drive\Pictures",
            @"F:\My Drive\Pictures",
            @"E:\"
        };

        // The image files stored in the image folder should be named accordingly:
        // [name]_[order of appearance].[time spent on image]
        // example: "event_1.8" (This image will appear first and stay on screen for 8 seconds)
        // This function returns the delay aspect from the name (if there is one)
        private int GetCustomImageDelay(string Name)
        {
            string FileName = Path.GetFileName(Name);
            string Delay_Number;
            try {
                Delay_Number = FileName.Split('_')[1].Split('.')[0];
            }
            catch (IndexOutOfRangeException) {
                Console.WriteLine("Could not get the delay from custom image.");
                return Default_Slide_Delay;
            }
            bool success = int.TryParse(Delay_Number, out int New_Delay);
            return (success) ? New_Delay : Default_Slide_Delay;
        }


        public delegate void ImageEventHandler(object sender, ImageEventArgs e);

        // This event is fired when the main window should refresh its data.
        public event ImageEventHandler RefreshInWindow;

        //This runs after the sliding animation has finished.
        public void SlidingCompleted(object sender, EventArgs e)
        {
            // Assume the default delay for the next animation.
            secondsToProgress = Default_Slide_Delay;

            if (currentSlidePosition == PicturePosition) { 
                UpdatePictureSlides(); // Check and update if our picture folder changed.
                currentPicture = 0; // Set value to 0 in case it doesn't get updated in a function.
            }

            // Refresh component information.
            if (currentSlidePosition == UpdatePosition) { 
                RefreshInWindow.Invoke(this, new ImageEventArgs(RefreshType.Component));
            }

            else if (currentSlidePosition > PicturePosition)
            {
                if (currentPicture >= imageFolderSize - 1) // Animation starts from the beginning
                {
                    rectTransform = new TranslateTransform(0, 0);
                    futureSecondsToProgress = GetCustomImageDelay(futureImageToProcess);
                    currentSlidePosition = 0;
                    currentPicture = 0;
                }
                else // Loop over the last two slides to create the illusion of several images
                {
                    UpdatePictureSlides();
                    currentSlidePosition = PicturePosition + 1;
                    rectTransform = new TranslateTransform(Screen_Positions[currentSlidePosition], 0);
                    
                }
                RefreshInWindow.Invoke(this, new ImageEventArgs(RefreshType.RectTransform));
                secondsToProgress = futureSecondsToProgress;
            }
        }

        // Returns an array of filenames in a directory.
        private String[] GetFilesFromDirectory(String searchFolder, String[] filters, bool isRecursive)
        {
            List<String> filesFound = new List<String>();
            var searchOption = isRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (var filter in filters)
            {
                filesFound.AddRange(Directory.GetFiles(searchFolder, String.Format("*.{0}", filter), searchOption));
            }
            return filesFound.ToArray();
        }

        // Returns array of filenames of image files in ImageFolderDirectories.
        private string[] RetrieveImageFiles() 
        {
            String[] files = new string[0];
            //Check for each directory specified in Path Array, and if found. Then get from that folder.
            foreach (string folder in ImageFolderDirectories)
            {
                if (Directory.Exists(folder))
                {
                    var filters = new String[] { "jpg", "jpeg", "png", "gif", "tiff", "bmp", "svg" };
                    files = GetFilesFromDirectory(folder, filters, false);
                    imageFolderSize = files.Length;
                    Array.Sort(files);
                    break;
                }
            }
            return files;
        }

        // Update the image variables with new images
        private void UpdatePictureSlides()
        {
            String[] files = RetrieveImageFiles();
            
            string FirstChosenPicture; // Link to our first picture file
            string SecondChosenPicture; // Link to our second picture file

            Uri PathToSecond;
            bool UsingDefault = false;

            // There are no custom images, simply select a random default one.
            if (files.Length < 1)
            {
                Random rand = new Random();
                FirstChosenPicture = @"/Resources/IdlePics/1_1 (" + Math.Min(rand.Next(1, 26), 25) + ").jpg";
                Uri Path_ToImage1 = new Uri(FirstChosenPicture, UriKind.Relative);
                FirstImage = new BitmapImage(Path_ToImage1);
                UsingDefault = true;
            }
            else if (files.Length > 1 && currentPicture + 1 < files.Length) // We have more than one custom image
            {
                FirstChosenPicture = files[currentPicture]; // Select first image
                SecondChosenPicture = files[currentPicture + 1]; // Select second image
                PathToSecond = new Uri(SecondChosenPicture);
                SecondImage = new BitmapImage(PathToSecond);
                futureImageToProcess = SecondChosenPicture;
            }
            else // We have only one custom image
            {
                FirstChosenPicture = files[currentPicture];
                futureImageToProcess = FirstChosenPicture;
            }

            Uri Path_ToImage = (UsingDefault) ?
                new Uri(FirstChosenPicture, UriKind.Relative) :
                new Uri(FirstChosenPicture);

            // Set All Local Picture Components
            FirstImage = new BitmapImage(Path_ToImage);
            if (files.Length <= 1) { SecondImage = new BitmapImage(Path_ToImage); }

            // Define the delay for slide
            futureSecondsToProgress = GetCustomImageDelay(FirstChosenPicture);
            currentPicture++;

            // Invoke refresh event, since pictures have changed.
            RefreshInWindow.Invoke(this, new ImageEventArgs(RefreshType.Image));
        }
    }
}
