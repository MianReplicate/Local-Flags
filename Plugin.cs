using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Lua;
using Lua.Proxy;
using MoonSharp.Interpreter;
using UnityEngine;
using UnityEngine.Networking;

namespace LocalFlags;

[BepInPlugin("netdot.mian.localflags", "Local Flags", "1.0.0")]
public class LocalFlags : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    internal const string Framework = "Custom Flag Framework";
    internal string CoverPath;
    internal string FlagsPath;
    internal string CustomFlagToTeamColorsPath;
    internal static Texture2D Cover;
    internal static List<Texture2D> Flags = new List<Texture2D>();
    
    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        
        var harmony = new Harmony("netdot.mian.patch");
        harmony.PatchAll();

        createModDirectoryIfNotExist();
        CoverPath = getPathForModDirectory("cover.png");
        FlagsPath = getPathForModDirectory("CustomFlags");
        CustomFlagToTeamColorsPath = getPathForModDirectory("CustomFlagToTeamColors");
        
        if (!Directory.Exists(FlagsPath))
        {
            Directory.CreateDirectory(FlagsPath);
        }

        if (!Directory.Exists(CustomFlagToTeamColorsPath))
        {
            // Directory.CreateDirectory(CustomFlagToTeamColorsPath);
        }

        StartCoroutine(LoadTextureAsync(CoverPath, texture => Cover = texture));

        var flags = Directory.GetFiles(FlagsPath);
        foreach(string flagPath in flags)
        {
            StartCoroutine(LoadTextureAsync(flagPath, texture => Flags.Add(texture)));
        }
        
        // Texture2Ds aren't normally exposed to RS so we have to expose it ourselves using the already provided TextureProxy class
        Script.GlobalOptions.CustomConverters.SetClrToScriptCustomConversion<Texture2D>((Func<Script, Texture2D, DynValue>) ((s, v) => DynValue.FromObject(s, (object) TextureProxy.New(v))));
    }

    private static void createModDirectoryIfNotExist()
    {
        if (!Directory.Exists(Path.Combine(Paths.PluginPath, "LocalFlags")))
        {
            Directory.CreateDirectory(Path.Combine(Paths.PluginPath, "LocalFlags"));
        }
    }

    private static string getPathForModDirectory(string path)
    {
        return Path.Combine(Paths.PluginPath, "LocalFlags", path);
    }
    private static IEnumerator LoadTextureAsync(string filePath, Action<Texture2D> onComplete, Action<float> onProgress = null)
    {
        // Convert file path to URI format
        string uri = "file:///" + filePath.Replace('\\', '/');
 
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(uri))
        {
            var asyncOp = www.SendWebRequest();
 
            // Report progress while loading
            while (!asyncOp.isDone)
            {
                onProgress?.Invoke(www.downloadProgress);
                yield return null;
            }
 
            if (www.result != UnityWebRequest.Result.Success)
            {
                Logger.LogError($"Error loading texture: {www.error} from {filePath}");
                onComplete?.Invoke(null);
                yield break;
            }
 
            // Get the downloaded texture
            Texture2D texture = DownloadHandlerTexture.GetContent(www);
            var array = filePath.Split('.');
            var fullName = "";
            for (var i = 0; i < array.Length - 1; i++)
            {
                fullName += array[i];
            }

            array = fullName.Split('\\');
            fullName = array[array.Length-1];
            texture.name = fullName;
            
            Logger.LogInfo($"Texture ({texture.name}) loaded successfully: {texture.width}x{texture.height} from {filePath}");
            onComplete?.Invoke(texture);
        }
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
                LocalFlags.Logger.LogInfo("Found framework! Adding local pack");
            
                var mutatorData = new Dictionary<string, object>();
                mutatorData.Add("name", "local");
                mutatorData.Add("cover", LocalFlags.Cover);
                mutatorData.Add("CustomFlags", LocalFlags.Flags);
                mutatorData.Add("CustomFlagToTeamColors", new ArrayList());
                
                framework.Call("addFlagPack", mutatorData);
            }
        }
    }
}
