# UnityGame
A game I'm making in Unity that currently includes unlimited terrain generation.

Project Outline (Private)
https://goo.gl/4ShZqV

# Welcome to the City Chunks readme!
#### This Commit Version: v0.0.6m_4
Unity Version: 5.6.2f1

## Installation / Setup
Windows x64 executable of v0.0.5m_2 available [here](https://dev.campbellcrowley.com/game/CityChunksv0.0.5m_2.zip).

Unity can be downloaded from its website at [unity3d.com](https://store.unity.com) (Free version is sufficient).

Open the Unity project in the corresponding version of Unity (Newer may work, but the versions described above are the latest tested versions). It may take a few minutes depending on your computer to open the project. Once open, open the Menu scene and click play in the Unity Editor.  

## Usage
### Starting the game from an executable build:
* Controls can be modified in the unity settings window in the "Input" tab.
* Graphics quality and resolution can be modified here as well in the other tab.
### Starting the game from the Unity editor:
* Ensure the Menu scene is open before clicking play.
* Game can also be exited by pressing `ctrl + p`.
### Playing the game once in the menu (Needs updating):
* Press "H" or click the "Host Game" button to start a game.
* Type the IP address of a hosted game to connect to an existing game, then click "Connect to host".
  - Default ip and port: dev.campbellcrowley.com:7777
  - Another example: 192.168.1.2:7777
  - Port will be 7777 unless port forwarding changes this.
* Press "S" to start a dedicated server without a player. Same as "H" but does not create a player or camera and does not render the scene or allow for input.
* Press "X" while game is paused or click the button in the top left while in game, to exit to the main menu and close a server.
### Default player controls are basic FPS controls:
* WASD to move.
* Shift to sprint.
* Left Ctrl to crouch.
* Space to jump.
* Mouse movement to move camera.
* V to switch between first and third person view.

## Issues
The project is only regularly tested on Windows 10. Other OS's may be supported, but are not tested. If you have any issues using any OS, please leave a bug report detailing any errors and any information that may help us reproduce the issue. We will try to get to bugs, but there is no guarantee that we will be able to fix all of them.

The game is intended to run on most modern computers. If you are experiencing issues, first try lowering the graphics quality settings. If the performance is still causing problems, posting an issue with details from the Unity profiler and information about your computer hardware, may help us begin to help solve an issue. This is not a main focus but ensuring the game is playable is definitely a priority.

Any issues with the Unity specifically, should not be directed to us, but instead to the [unity forums](https://forum.unity3d.com/).  

##
Copyright 2017 by Campbell Crowley. All Rights Reserved.
Contact Information: github@campbellcrowley.com.
