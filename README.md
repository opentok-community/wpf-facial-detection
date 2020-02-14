WPF Facial Detection
=====================

This project uses the custom video renderer features in the OpenTok Windows SDK. With it you'll be able to add your first bit of Computer Vision to your application - facial detection

*Important:* To use this application, follow the instructions in the
[Quick Start](../README.md#quick-start) section of the main README file
for this repository.

## Quick Start

1. Get values for your OpenTok **API key**, **session ID**, and **token**.

   You can obtain these values from your [TokBox account](#https://tokbox.com/account/#/).
   Make sure that the token isn't expired.

   For testing, you can use a session ID and token generated at your TokBox account page.
   However, the final application should obtain these values using the [OpenTok server
   SDKs](https://tokbox.com/developer/sdks/server/). For more information, see the OpenTok
   developer guides on [session creation](https://tokbox.com/developer/guides/create-session/)
   and [token creation](https://tokbox.com/developer/guides/create-token/).

2. In Visual Studio, open the .sln solution file for the sample app you are using CustomVideoRenderer.sln

3. Open the MainWindow.xaml.cs file for the app and edit the values for `API_KEY`, `SESSION_ID`,
   and `TOKEN` to match API key, session ID, and token data you obtained in step 1.

NuGet automatically installs the OpenTok SDK when you build the project.

**Test on non-development machines**: OpenTok SDK includes native code that depends on
[Visual C++ Redistributable for Visual Studio 2015](https://www.microsoft.com/en-us/download/details.aspx?id=48145
"Visual C++ Redistributable for Visual Studio 2015").  It's probably
already installed on your development machine but not on test
machines.  Also, you may need 32-bit version even if all your code is
AnyCPU running on a 64-bit OS.

## EMGU CV Licence

Please read the [Emgu Cv Licence](http://www.emgu.com/wiki/index.php/Emgu_TF_License) They have a dual licence system for Open Source vs Commercial Closed Source.
