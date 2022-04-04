# Distant Worlds 2 Mod Loader & .NET 6 Launcher

Thank you for supporting [Distant World 2 Modding Community](https://github.com/DW2MC) efforts. :slightly_smiling_face:

Just extract this package to your game directory, e.g. `...\Steam\steamapps\common\Distant Worlds 2`.


## What are all these files?

The following files are the main course of this package.

|              File              | Description                                                                   |
|:------------------------------:|:------------------------------------------------------------------------------|
| `DistantWorlds2.ModLoader.dll` | The mod loader and runtime mod manager.                                       |
|  `DistantWorlds2.exe.config`   | This lets the mod loader work when not using the .NET 6 launcher.             |
|        `DW2Net6Win.exe`        | The .NET 6 launcher. This runs the game under .NET 6 instead of .NET 4.       |


### What about the .cmd files?

These are executable scripts used to run the game under different configurations.

|            File            | Description                                                                        |
|:--------------------------:|:-----------------------------------------------------------------------------------|
| `DistantWorlds2-Debug.cmd` | This runs the game with the Mod Loader's debug mode enabled.                       |
|   `DW2Net6Win-Debug.cmd`   | This launches the .NET 6 launcher with the Mod Loader's debug mode enabled.        |
|    `DW2Net6Win-PGO.cmd`    | This launches the .NET 6 launcher with Profile Guided Optimizations (PGO) enabled. |


### What about the .reg files?

These are registry patches used to make it easier to run the game under different configurations.

|         File          | Description                                                                                              |
|:---------------------:|:---------------------------------------------------------------------------------------------------------|
|   `DW2Net6Win.reg`    | This sets the .NET 6 launcher to run instead of DistantWorlds2.exe by Steam/GoG/etc.                     |
| `DW2Net6Win-PGO.reg`  | This sets the .NET 6 launcher w/ PGO enabled to run instead of `DistantWorlds2.exe` by Steam/GoG/etc.    |
| `DW2Net6Win-Undo.reg` | This undoes what the other two change such that the normal `DistantWorlds2.exe` is run by Steam/GoG/etc. |


### What about that empty `mods` folder?

This is where you will extract your modifications.

Extracting this package to your game directory just creates the directory for you.
