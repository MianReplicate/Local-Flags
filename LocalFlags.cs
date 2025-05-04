using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using ImageMagick;
using Lua;
using Lua.Proxy;
using MoonSharp.Interpreter;
using Ravenfield.Mutator.Configuration;
using UnityEngine;
using UnityEngine.Networking;
using Color = UnityEngine.Color;

namespace LocalFlags;

[BepInPlugin("netdot.mian.localflags", "Local Flags", "2.1.1")]
public class LocalFlags : BaseUnityPlugin
{
    public new static ManualLogSource Logger;
    internal const string Framework = "Custom Flag Framework";
    internal static readonly List<Pack> Packs = [];
    
    private void Awake()
    {
        Time.timeScale = 0;
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Local Flags is gonna local all over you! :wink:");
        
        var harmony = new Harmony("netdot.mian.patch");
        harmony.PatchAll();
        
        CustomPaths.Init();
        
        var packPaths = Directory.GetDirectories(CustomPaths.PacksPath);
        foreach (var packPath in packPaths)
        {
            var arr = packPath.Split('\\');
            var packName = arr[arr.Length - 1];
            
            Packs.Add(new Pack(packPath, packName));
        }
        
        // Texture2Ds aren't normally exposed to RS so we have to expose it ourselves using the already provided TextureProxy class
        Script.GlobalOptions.CustomConverters.SetClrToScriptCustomConversion<Texture2D>((s, v) => DynValue.FromObject(s, TextureProxy.New(v)));
        Time.timeScale = 1;
    }

    public static Texture2D LoadTexture(string filePath)
    {
        Texture2D texture = null;
        try
        {
            texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(filePath));
            texture.name = Path.GetFileNameWithoutExtension(filePath);
            texture.Compress(false);
            texture.Apply(false, true);
            
            Logger.LogInfo($"Texture ({texture.name}) loaded successfully: {texture.width}x{texture.height} from {filePath}");
        }
        catch (Exception e)
        {
            Logger.LogError($"Failed to load texture ({filePath}): {e.Message}");
        }
        return texture;
    }
}

public class Pack
{
    private static readonly Texture2D DefaultCover = LocalFlags.LoadTexture(CustomPaths.DefaultCoverPath);

    private readonly string _name;
    private readonly Texture2D _cover;
    private readonly Texture2D[] _flags;
    private readonly Color[] _flagsToTeamColors;
    private readonly string _directory;
    private readonly MutatorEntry _mutatorEntry;

    public Pack(string directory, string name)
    {
        _name = name;
        _directory = directory;
        if (File.Exists(_directory + "\\cover.png"))
        {
            _cover = LocalFlags.LoadTexture(_directory + "\\cover.png");
        }
        else if(File.Exists(_directory + "\\cover.jpg"))
        {
            _cover = LocalFlags.LoadTexture(_directory + "\\cover.jpg");
        }

        if (_cover == null)
        {
            _cover = DefaultCover;
        }
        
        _mutatorEntry = new MutatorEntry
        {
            name = _name,
            menuImage = _cover,
            configuration = new MutatorConfiguration(),
            description = "A local pack from Local Flags!"
        };
        
        var toOptimize = GetOptimizeFlagFiles();

        if (toOptimize != null)
        {
            foreach (var flagPath in toOptimize)
            {
                LocalFlags.Logger.LogInfo($"Optimizing texture: {Path.GetFileNameWithoutExtension(flagPath)}");
                try
                {
                    var bytes = File.ReadAllBytes(flagPath);
                    IMagickImage<byte> image = new MagickImage(bytes);
                    image.SetCompression(CompressionMethod.DXT3);
                    if (image.Width > 2000 && image.Height > 2000)
                    {
                        image.Resize(new Percentage(25));
                    }
                    image.Write(_directory + @"\CustomFlags\" + Path.GetFileNameWithoutExtension(flagPath) + ".jpg", MagickFormat.Jpeg);
                    image.Dispose();
                    File.Delete(flagPath);
                    Resources.UnloadUnusedAssets();
                    LocalFlags.Logger.LogInfo($"Finished optimizing: {Path.GetFileNameWithoutExtension(flagPath)}");
                }
                catch (Exception e)
                {
                    LocalFlags.Logger.LogError($"Failed to optimize texture. The format might be unsupported: {e.Message}");
                }
            }
        }
        
        var flags = GetFlagFiles();
        var colorPaths = GetColorFiles();
        var colors = colorPaths.ToList();
        if (flags != null)
        {
            _flagsToTeamColors = new Color[flags.Length];
            _flags = new Texture2D[flags.Length];
            for(var i = 0; i < flags.Length; i++)
            {
                var flagPath = flags[i];
                var texture = LocalFlags.LoadTexture(flagPath);
                if (texture != null)
                {
                    texture.name = texture.name.ToUpper();
                    _flags[i] = texture;
                    
                    var colorIndex = colors.FindIndex(color => Path.GetFileNameWithoutExtension(color).ToUpper().Equals(texture.name));
                    var pathToUse = colorIndex != -1 ? colors[colorIndex] : flagPath;
                    
                    IMagickImage<byte> image = new MagickImage(File.ReadAllBytes(pathToUse));
                    IMagickColor<byte> pixel;
                    if (colorIndex == -1)
                    {
                        image.Resize(1, 1);
                        pixel = image.GetPixels()[0, 0].ToColor();
                        image.Resize(15, 15);
                        image.Write(_directory + @"\CustomFlagToTeamColors\" + texture.name + ".jpeg", MagickFormat.Jpeg);
                    }
                    else
                    {
                        pixel = image.GetPixels()[0, 0].ToColor();
                    }
                    
                    var unityColor = new Color((float) pixel.R / 255, (float) pixel.G / 255, (float) pixel.B / 255);
                    image.Dispose();
                    
                    _flagsToTeamColors[i] = unityColor;
                    LocalFlags.Logger.LogInfo($"Added color ({unityColor.r}, {unityColor.g}, {unityColor.b}) for {texture.name}");   
                }

                Resources.UnloadUnusedAssets();
            }
        }
    }

    public string GetName()
    {
        return _name;
    }

    public Texture2D GetCover()
    {
        return _cover;
    }

    public Texture2D[] GetFlags()
    {
        return _flags;
    }

    public List<Color> GetFlagsToTeamColors()
    {
        return _flagsToTeamColors.ToList();
    }

    public MutatorEntry GetMutatorEntry()
    {
        return _mutatorEntry;
    }

    private string[] GetOptimizeFlagFiles()
    {
        if (!Directory.Exists(_directory + "\\OptimizeFlags"))
        {
            Directory.CreateDirectory(_directory + "\\OptimizeFlags");
        }
        return Directory.GetFiles(_directory + "\\OptimizeFlags");
    }

    private string[] GetFlagFiles()
    {
        if (!Directory.Exists(_directory + "\\CustomFlags"))
        {
            Directory.CreateDirectory(_directory + "\\CustomFlags");
        }
        
        return Directory.GetFiles(_directory + "\\CustomFlags");
    }

    private string[] GetColorFiles()
    {
        if (Directory.Exists(_directory + "\\CustomFlagToTeamColors"))
        {
            Directory.CreateDirectory(_directory + "\\CustomFlagToTeamColors");
        }
        
        return Directory.GetFiles(_directory + "\\CustomFlagToTeamColors");
    }
}

[HarmonyPatch(typeof(ModManager))]
[HarmonyPatch(nameof(ModManager.OnGameManagerStart))]
class AddBuiltInMutatorPatch
{
    static void Prefix(ModManager __instance)
    {
        foreach (var pack in LocalFlags.Packs)
        {
            // no idea why but the game object seems to just die after a while for some reason? So we have to add it again to prevent an error
            pack.GetMutatorEntry().mutatorPrefab = new GameObject(pack.GetName());
            __instance.builtInMutators.Add(pack.GetMutatorEntry());
        }
    }
}

[HarmonyPatch(typeof(ModManager))]
[HarmonyPatch(nameof(ModManager.SpawnAllEnabledMutatorPrefabs))]
class MutatorLoadPatch
{
    static void Prefix()
    {
        foreach (var pack in LocalFlags.Packs)
        {
            // no idea why but the game object seems to just die after a while for some reason? So we have to add it again to prevent an error
            pack.GetMutatorEntry().mutatorPrefab = new GameObject(pack.GetName());
        }
    }
    static void Postfix()
    {
        LocalFlags.Logger.LogInfo("MODMANAGER JUST CREATED DA PREFABS!");
        var prefab = GameObject.Find(LocalFlags.Framework);
        if (prefab && ScriptedBehaviour.GetScript(prefab))
        {
            var framework = ScriptedBehaviour.GetScript(prefab);

            LocalFlags.Logger.LogInfo("Found framework! Adding local packs.");

            var mutatorEntries = ModManager.GetAllEnabledMutators();
            foreach (var pack in LocalFlags.Packs)
            {
                if (mutatorEntries.Contains(pack.GetMutatorEntry()))
                {
                    LocalFlags.Logger.LogInfo($"Adding local pack: {pack.GetName().ToUpper()}");
                    Dictionary<string, object> mutatorData = [];
                    mutatorData.Add("name", pack.GetName());
                    mutatorData.Add("cover", pack.GetCover());
                    mutatorData.Add("CustomFlags", pack.GetFlags());
                    mutatorData.Add("CustomFlagToTeamColors", pack.GetFlagsToTeamColors());

                    framework.Call("addFlagPack", mutatorData);
                }
            }

        }
        else
        { 
            LocalFlags.Logger.LogInfo("No framework found :<");
        }
    }
}
