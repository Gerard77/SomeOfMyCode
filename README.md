# SomeOfMyCode
A taste of a .NET 5 Client - Server desktop app I developed in C#. This was an app I developed to control remotely a robotic vehicle using a driving wheel connected to the Client. The Robot would send back information like camera record in real time and information of its hardware. The frontend was developed in WPF using MVVM pattern, where the ViewModel was the main frontend script. I also developed the Client - Server communications using microservices (gRPC) as well as the Server - Robot communication using ROS2 (Robotic Operating System).

In this repo I present three scripts to show my programming capabilities: CompactLayout.xaml, CompactLayout.xaml.cs and MainViewModel.cs.
They won't work on they own as very important parts of the app are not present like the services declaration, model, tools, the custom Logger I made, as well as the Server part.

-> CompactLayout.xaml: The design of the View that would display the cameras in real time as well as the hardware status, gps position, velocity, custom log viewer with capabilities like ordering or searching, etc. It looks like this:

![ksampol](https://user-images.githubusercontent.com/28361405/156815795-300892dd-8381-4bcf-8526-c1d9fb185d45.PNG)

-> CompactLayout.xaml.cs: The code-behind of the View. Here we can find things like route loading (route to send to the Robot), pin positioning on the map, item selection from the log viewer...

-> MainViewModel.cs: The ViewModel that controls lots (don't get scared, it's almost 3000 code lines). Some of the things that it does is controlling the data sent to/from the CommpactLayout, connect to the Communications part of the Client (ClientProxy), changing from single screen to triple screen (each camera in a screen, cooler display). You can also find functionalities like image type conversion to display the images coming from the camera or accessing the driving wheel inputs and API. You can also see use of threads, async programming, patterns like Singleton, error handling, static variables and classes or EventHandlers.
