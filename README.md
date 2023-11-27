# SotunkiTV - Open Source Information TV Program

Welcome to the official documentation for the SotunkiTV Project, a dynamic and versatile information TV program designed and developed using Windows Presentation Foundation (WPF) in Microsoft Visual Studio.
Overview

SotunkiTV is a single executable application that operates in fullscreen mode, seamlessly cycling through various animations. This project integrates diverse functionalities to display real-time information, making it a valuable asset for information dissemination.
Features

### SotunkiTV currently showcases the following data:

    Latest Instagram Post: Displays the most recent post from the account associated with the provided API key.

    Meal Information: Provides details about the scheduled meal and vegan meal for the current day.

    Weather Information: Offers the current day's weather conditions.

    HSL Bus Stops: Displays the scheduled arrival times for defined HSL bus stops.

    Custom Images: Presents custom images from specified folders.

## Dependencies

To enable these features, SotunkiTV relies on the following dependencies:

    Instagram: Utilizes Meta's Instagram API (API key required).

    Food Information: Uses Web Scraping with HtmlAgilityPack.

    Weather Information: Retrieves data from OpenWeatherMap (no API key required).

    Bus Schedules: Fetches information from the DigiTransit API (API key required).

## Setup 

To ensure proper functionality, place the necessary files in the correct directories:

    Instagram API Key: Save the API Token in a text file located at C:\Sotunki-TV\account.txt. For guidance on obtaining the Instagram API Token, refer to this guide.

    DigiTransit API Key: Save the API key in a text file at C:\Sotunki-TV\digitransit.txt. Follow this guide for obtaining the DigiTransit API key.

    Custom Images: Place images in any of the specified directories:
        "G:\Oma Drive\Pictures"
        "F:\Oma Drive\Pictures"
        "G:\My Drive\Pictures"
        "F:\My Drive\Pictures"
        "E:"

    Adhere to the file naming convention: [name]_[order]_[duration]. For example, "pikkujoulut_1_8" indicates the first custom image with a display duration of 8 seconds.

## Project Structure

The project is organized into distinct classes: HSL.cs, Images.cs, Instagram.cs, Vantti.cs, and Weather.cs. The MainWindow.cs orchestrates these classes, calling their respective fetch functions for seamless integration.
## Feedback

SotunkiTV relies on various APIs and factors that may evolve over time. Your feedback and reporting of any issues are essential to maintaining the project's vitality. If you identify outdated code or have improvements to suggest, please submit a pull request to contribute to the ongoing development of SotunkiTV. Thank you for your support!
