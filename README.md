# Empire-Mod
This is the repository for the Empire mod Ludeon Studio's topdown base building-exploration game RimWorld.
The Mod features the ability to found and manage your own empire in order to assist the player in his daily endeavours.
This mod was initially developed by Saakra, a lone Mod Dev. He has since moved on due to IRL issues and handed over the reigns of development to the community.

## Building
To build using the dotnet cli, go to `[version]/Source/FactionColonies` and run `dotnet build`. The Assembly will output to the Assemblies folder, along with debug symbols.

`dotnet build --configuration release`

The Assembly will output to the Assemblies folder. 
Debug symbols will only be included on the debug configuration, 
which requires RimWorldData_(version) to be copied from the vanilla game.
