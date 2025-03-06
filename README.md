# Introduction
Local Flags is a BepInEx mod for Ravenfield that interacts with the [Custom Flags Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=3385310995) to allow users to add flags locally without needing to make or install their own flag pack mutators. As such this removes the need for downloading or learning external tools like the Ravenfield modding tools or the Unity Editor.

Subscribing to this WILL not give you the mod. Instead you will get a mutator that does jack shit.

# Why Release on GitHub?!
- A proper release cannot be provided on the workshop because this is a BepInEx mod that would otherwise not exist due to the limitations of RS. Follow the instructions to install this.

## Why should I use this over creating my own flag pack mutators?
- Removes the need for learning the Unity Editor or Ravenfield modding tools.
- A much simpler process than creating your own flag packs. Adding custom flags is much faster with this.

## Why should I NOT use this over flag pack mutators?
- There is a longer load time depending on how many flags you have BUT it does not add as much load time assuming everything has been cached and is optimized. Loading times can be heavily improved if you optimize the flags with instructions in the FAQ. ~30 second load time with 1000+ flags at resolutions 1250x750 or lower.
- These cannot be published to the Steam workshop! You need to create a flag pack mutator with Ravenfield modding tools in order to publish your packs.

# Installation

1. Install [BepInEx 5](https://github.com/BepInEx/BepInEx) into Ravenfield.
2. Download the latest release for [Local Flags](https://github.com/MianReplicate/Local-Flags/releases).
3. Extract zip into ```Ravenfield\BepInEx\plugins```
4. Open the newly extracted folder, open "Local" and drag any flag textures (jpg and png only) you want into the "CustomFlags" folder. I recommend optimizing your images beforehand or dropping them into the "OptimizeFlags" folder if you are too lazy.
5. Launch Ravenfield and enjoy your new flags!

**(Optional Steps)**

1. Assign colors to your flags by creating images (same name as the flag) within the "CustomFlagToTeamColors" folder. This is required so that when a flag is selected for a team, we can adjust the color accordingly. If you do not provide a solid image color for a flag, one will be generated.
2. If you want, you can add a cover inside of the "Local" folder. This will replace the default cover. Make sure your file is named "cover" and is a jpg or png file.
3. If you want to, you can create multiple local packs. Duplicate the "Local" folder and rename it. This name will be the name of the pack in game. You now have another pack that you can separate flags into and a different cover for!

# FAQ

## How do I update the mod?
- If you already have a "LocalFlags" folder in your plugins folder, all you need to do is replace the dlls within the "LocalFlags" folder with the dlls from the new release. This effectively updates the mod.

## My game is frozen upon load!
- If you have a lot of flags this may happen. The mod freezes the game intentionally to let all the flags load first. The game will unfreeze once the flags have all loaded. If you want to confirm that the mod is installing the flags, open ```Ravenfield\BepInEx\config\BepInEx.cfg``` and enable the console at the category ```Logging.Console```. When you next load Ravenfield, a command prompt window will open that shows a bunch of text. After a few seconds or so, Local Flags will start logging progress of loading flags.
- If you just optimized your flag textures or had the mod generate colors for you, the game will for some reason freeze for a while depending on how much was done. This does not happen upon next game load. Don't ask me why it happens, I don't know.
- If the game is stuck on loading mutators, and you've confirmed that the mod has finished loading the flags, you may need to restart the game. I have attempted to fix this issue, but I do not know for sure if it truly is fixed.

## Flags are taking forever to load!
- Try throwing your flags into the "OptimizeFlags" folder. The mod will compress your flags for you and downsize if your flags are terribly big. After it optimizes them, it automatically places them in "CustomFlags" (Make sure you have copies! This will remove the images within the folder as they are optimized). Optimizing will take some time depending on the resolution of the flag.
- If colors are not already provided for the flags in CustomFlagToTeamColors, the mod has to generate them which can take some time depending on the resolution of your flags. If this is taking forever, you should consider optimizing your flag textures as instructed in the above line!
- If you want to see the progress of the mod loading/generating the flags/colors, turn on the console as instructed in the FAQ question "My game is frozen upon load".

## My computer crashed/The game crashed!
- If this mod is crashing your game or computer, chances are you are running out of memory. Check your Task Manager to confirm as Ravenfield is loading. Consider optimizing your flags or removing some. If you want the best memory efficiency, you should turn these into a flag pack mutator.

## Does this work on Linux or other CPU architectures?
- No idea. I've attempted to keep compatibility with what I have used so they should work, but I have no way to test.

# Credits
- [amir16yp](https://github.com/amir16yp) for the Unity request texture downloader used to load flag textures
