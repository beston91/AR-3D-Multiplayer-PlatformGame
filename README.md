# PlacenoteSDK SavePlanes
Save and Load all the flat planes (horizontal and vertical) once detected in an environment, so your user doesn’t have to keep walking around detecting them at home or at the office! 

![Loading Planes from a saved map](https://i.imgur.com/Dfy8jo5.gif)

This is a sample app that uses PlacenoteSDK and ARKit’s plane detection to save and load plane meshes within a Unity scene. PlacenoteSDK uses advanced computer vision and scene recognition technology to remember mobile phone locations for persistent augmented reality. Each scene is saved as a map file, uploaded to the cloud accessible to all other phones and can have associated meta-data. 

In this app we save all the planes we find using ARKit’s plane detection as vertices in the meta-data of the map! It uses Unity's mesh type to save meshes (or plane types if iOS 11.2 or older). 


## Getting Started
To install PlacenoteSDK-SavePlanes, follow these instructions:

* Clone this repository
* Critical library files are stored using lfs, which is the large file storage mechanism for git.
  * To Install these files, install lfs either using HomeBrew:
  
     ```Shell Session 
     brew install git-lfs
     ```

      or MacPorts: 
      ```Shell Session
      port install git-lfs
      ```
   
  * And then, to get the library files, run: 
     ```Shell Session
     git lfs install 
     git lfs pull
     ```
* Open the project as a new project in Unity (Recommended: Unity 2017)
* Make sure you have an API key. [Get your API key here](https://developer.placenote.com)
* To build and run the project on your iOS device, follow these [Build Instructions](https://placenote.com/install/unity/build-unity-project/)

