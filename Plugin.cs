﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Lua;
using Lua.Proxy;
using MoonSharp.Interpreter;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UnityEngine;
using UnityEngine.Networking;
using Color = UnityEngine.Color;

namespace LocalFlags;

[BepInPlugin("netdot.mian.localflags", "Local Flags", "1.1.0")]
public class LocalFlags : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    internal const string Framework = "Custom Flag Framework";
    internal static List<Pack> packs = new List<Pack>();
    internal static int CurrentlyDownloading;
    internal static int QueuedLoads;
    internal static float PrevTimeScale;
    public static LocalFlags Instance;
    
    private void Awake()
    {
        Instance = this;
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        
        var harmony = new Harmony("netdot.mian.patch");
        harmony.PatchAll();
        
        ModPaths.Init();
        
        var packPaths = Directory.GetDirectories(ModPaths.PacksPath);
        foreach (var packPath in packPaths)
        {
            string[] arr = packPath.Split('\\');
            string packName = arr[arr.Length - 1];
            
            packs.Add(new Pack(packPath, packName));
        }
        
        // Texture2Ds aren't normally exposed to RS so we have to expose it ourselves using the already provided TextureProxy class
        Script.GlobalOptions.CustomConverters.SetClrToScriptCustomConversion<Texture2D>((s, v) => DynValue.FromObject(s, TextureProxy.New(v)));
    }

    private void Update()
    {
        if (QueuedLoads > 0)
        {
            PrevTimeScale = PrevTimeScale == 0 ? Time.timeScale : PrevTimeScale;
            Time.timeScale = 0;
        } else
        {
            Time.timeScale = PrevTimeScale;
        }
    }

    public void QueueTextureAsync(string filePath, Action<Texture2D> onComplete)
    {
        StartCoroutine(LoadTextureAsync(filePath, onComplete));
    }

    public Texture2D LoadTexture(string filePath)
    {
        QueuedLoads++;
        Texture2D texture = null;
        try
        {
            texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(filePath));
            texture.name = Path.GetFileNameWithoutExtension(filePath);
            texture.Compress(false);
        }
        catch (Exception e)
        {
            Logger.LogError($"Failed to load texture ({filePath}): "+e);
        }
        QueuedLoads--;
        return texture;
    }
    
    public IEnumerator LoadTextureAsync(string filePath, Action<Texture2D> onComplete)
    {
        QueuedLoads++;
        while (CurrentlyDownloading >= 10)
        {
            yield return null;
        }
        CurrentlyDownloading++;
        Logger.LogInfo($"Currently loading {CurrentlyDownloading} textures..");
        
        // convert filePath
        var convertedFilePath = "file:///" + filePath.Replace("\\", "/");

        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(convertedFilePath))
        {
            var async = www.SendWebRequest();
            while (!async.isDone)
            {
                yield return null;
            }

            if (www.result != UnityWebRequest.Result.Success)
            {
                Logger.LogError($"Failed to load {filePath}: {www.error}");
                yield break;
            }
            
            Texture2D texture = DownloadHandlerTexture.GetContent(www); 
            texture.name = Path.GetFileNameWithoutExtension(filePath);
            texture.Compress(false);
        
            Logger.LogInfo($"Texture ({texture.name}) loaded successfully: {texture.width}x{texture.height} from {filePath}");
            onComplete?.Invoke(texture);
        }
        CurrentlyDownloading--;
        QueuedLoads--;
    }
}

public class Pack
{
    public static readonly Texture2D DefaultCover = LocalFlags.Instance.LoadTexture(ModPaths.DefaultCoverPath);

    private readonly String _name;
    private readonly Texture2D _cover;
    private List<Texture2D> _flags = new List<Texture2D>();
    private Color[] _flagsToTeamColors;
    private readonly String _directory;

    public Pack(String directory, String name)
    {
        _name = name;
        _directory = directory;
        if (File.Exists(_directory + "\\cover.png"))
        {
            _cover = LocalFlags.Instance.LoadTexture(_directory + "\\cover.png");
        }
        else if(File.Exists(_directory + "\\cover.jpg"))
        {
            _cover = LocalFlags.Instance.LoadTexture(_directory + "\\cover.jpg");
        }

        if (_cover == null)
        {
            _cover = DefaultCover;
        }
        
        var flags = GetFlagFiles();
        var colorPaths = GetColorFiles();
        var colors = new List<String>();
        foreach(var colorPath in colorPaths)
        {
            colors.Add(Path.GetFileNameWithoutExtension(colorPath));
        }

        if (flags != null)
        {
            _flagsToTeamColors = new Color[flags.Length];
            for(int i = 0; i < flags.Length; i++)
            {
                String flagPath = flags[i];
                LocalFlags.Instance.QueueTextureAsync(flagPath, texture =>
                {
                    
                    texture.name = texture.name.ToUpper();
                    AddFlag(texture);
                    int flagLocation = _flags.IndexOf(texture);

                    var colorIndex = colors.FindIndex(color => color.ToUpper() == texture.name);
                    if (colorIndex != -1)
                    {
                        using (var image = Image.Load<Rgba32>(colorPaths[colorIndex]))
                        {
                            var color = image[0, 0];
                            var unityColor = new Color((float) color.R / 255, (float) color.G / 255, (float) color.B / 255);

                            _flagsToTeamColors[flagLocation] = unityColor;
                            LocalFlags.Logger.LogInfo($"Added color ({unityColor.r}, {unityColor.g}, {unityColor.b}) for {texture.name}");
                        }
                    }
                });
            }
        }
    }

    public String GetName()
    {
        return _name;
    }

    public Texture2D GetCover()
    {
        return _cover;
    }

    public List<Texture2D> GetFlags()
    {
        return _flags;
    }

    public List<Color> GetFlagsToTeamColors()
    {
        return _flagsToTeamColors.ToList();
    }

    public void AddFlag(Texture2D flag)
    {
        _flags.Add(flag);
    }

    public String[] GetFlagFiles()
    {
        if (Directory.Exists(_directory + "\\CustomFlags"))
        {
            return Directory.GetFiles(_directory + "\\CustomFlags");
        }

        return null;
    }

    public String[] GetColorFiles()
    {
        if (Directory.Exists(_directory + "\\CustomFlagToTeamColors"))
        {
            return Directory.GetFiles(_directory + "\\CustomFlagToTeamColors");
        }

        return null;
    }
}

[HarmonyPatch(typeof(ModManager))]
[HarmonyPatch(nameof(ModManager.SpawnAllEnabledMutatorPrefabs))] // if possible use nameof() here
class ModManagerPatch
{

    static void Postfix()
    {
        LocalFlags.Logger.LogInfo("MODMANAGER JUST CREATED DA PREFABS!");
        var prefab = GameObject.Find(LocalFlags.Framework);
        if (prefab)
        {
            var framework = ScriptedBehaviour.GetScript(prefab);
            if (framework)
            {
                LocalFlags.Logger.LogInfo("Found framework! Adding local packs.");

                foreach (var pack in LocalFlags.packs)
                {
                    LocalFlags.Logger.LogInfo($"Adding local pack: {pack.GetName().ToUpper()}");
                    var mutatorData = new Dictionary<string, object>();
                    mutatorData.Add("name", pack.GetName());
                    mutatorData.Add("cover", pack.GetCover());
                    mutatorData.Add("CustomFlags", pack.GetFlags());
                    mutatorData.Add("CustomFlagToTeamColors", pack.GetFlagsToTeamColors());
                
                    framework.Call("addFlagPack", mutatorData);   
                }
            }
            else
            {
                LocalFlags.Logger.LogInfo("No framework found :<");
            }
        }
    }
}
