# HelloGladios

Example project for creating Gladio Mori mods.

To get it working. Delete the assembly dependencies and add them from your own machine.

Right click Dependencies->Add Project Reference->Browse->Browse...->Select the required dlls one at a time
Following assemblies are found inside your Gladio Mori installation in "Gladio Mori_Data\Managed":
- Assembly-CSharp
- UnityEngine
- UnityEngine.CoreModule
- UnityEngine.UI
- Mirror
- UnityEngine.InputLegacyModule



## To test locally:
- Build your project
- Download the harmony mod zip manually from: https://mod.io/g/gladio-mori/m/harmonyxpack#description
- Go to the root of your Gladio Mori installation.
- Create Folder "Mods"
- Create subfolder for the harmony mod and unzip the contents there. Make sure the harmony mod is alphabetically before your mod. Name the subfolder: "Mods\0HarmonyX"
- Create another subfolder inside the "Mods" folder for your mod "MyMod"
- Copy your dll to "Mods\MyMod" from bin
- Start Gladio Mori

If everything went right, your mod should be working.

## Uploading content to mod.io:
- Add your files to a zip with no subfolders. Include the gm.modcfg file inside your zip. It makes sure the harmony mod is loaded before yours.
- Create the mod in mod.io
- Select relevant tags:
- Moveset:
  - Should contain one or more moveset files with ".json" extension
- Player skin:
  - Should contain one or more custom texture files with ".jpg", ".jpeg" or ".png" extension
- Local mod:
  - Should contain a dll mod that is not required by the clients in a multiplayer game. Server side mods even when altering multiplayer experience should be in this category
- Multiplayer mod:
  - Mods that are required by the clients and should be automatically downloaded.
- Arena:
  - Should contain a asset bundle called "maps". These are automatically loaded by clients when joining a server.
- Equipment:
  - Should contain a asset bundle called "equipment". These are automatically loaded by clients when joining a server.
- IMPORTANT: add HarmonyXPack as a dependency to your mod

## To send messages between clients and server:
- Define a struct that implements IMultiplayerModMessage
- Make sure the GetMessageID returns a random value that is unique to your message and mod
  - If the value conflicts with another, there will be errors if both mods are used at the same time
- Register a function to receive your message using MultiplayerModMessageManager.RegisterServerMessageHandler or MultiplayerModMessageManager.RegisterClientMessageHandler
- Your registered function will receive a byte array that you can then deserialize using MultiplayerModMessageManager.Deserialize()

## The mod load order is:
- Load modio subscribed maps
- Load modio subscribed equipment
- Load all mods from local mods folder in alphabetical order
- Load modio subscribed dll mods

## Dependencies:
- If you want to have dependencies to other mods, look at DependencyExample folder
