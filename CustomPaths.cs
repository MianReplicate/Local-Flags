﻿using System.IO;
using BepInEx;

namespace LocalFlags;

public class CustomPaths
{
    public static string DefaultCoverPath;
    public static string PacksPath;
    public static void CreateModDirectoryIfNotExist()
    {
        if (!Directory.Exists(Path.Combine(Paths.PluginPath, "LocalFlags")))
        {
            Directory.CreateDirectory(Path.Combine(Paths.PluginPath, "LocalFlags"));
        }
    }

    public static string GetPathForModDirectory(string path)
    {
        return Path.Combine(Paths.PluginPath, "LocalFlags", path);
    }

    public static void Init()
    {
        CreateModDirectoryIfNotExist();
        DefaultCoverPath = GetPathForModDirectory("cover.png");
        PacksPath = Path.Combine(Paths.PluginPath, "LocalFlags");
        
        if (!Directory.Exists(PacksPath))
        {
            Directory.CreateDirectory(PacksPath);
        }

    }
}