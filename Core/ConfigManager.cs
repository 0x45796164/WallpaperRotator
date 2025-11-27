using System;
using System.IO;
using System.Text.Json;
using WallpaperRotator.Core.Models;
using WallpaperRotator.Utilities;

namespace WallpaperRotator.Core;

public static class ConfigManager
{
    private static readonly string CONFIG_FOLDER = AppDomain.CurrentDomain.BaseDirectory;
    
    private static readonly string CONFIG_FILE_PATH = Path.Combine(CONFIG_FOLDER, "config.json");
    
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static AppConfig CurrentConfig { get; private set; } = new();

    public static AppConfig LoadConfig()
    {
        try
        {
            if (!Directory.Exists(CONFIG_FOLDER))
            {
                Directory.CreateDirectory(CONFIG_FOLDER);
            }

            if (!File.Exists(CONFIG_FILE_PATH))
            {
                Logger.Info("Config file not found. Creating default.");
                var defaultConfig = new AppConfig();
                SaveConfig(defaultConfig);
                CurrentConfig = defaultConfig;
                return defaultConfig;
            }

            string json = File.ReadAllText(CONFIG_FILE_PATH);
            var config = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions);
            
            if (config == null)
            {
                Logger.Warn("Config file was empty or invalid. returning new default.");
                CurrentConfig = new AppConfig();
                return CurrentConfig;
            }

            CurrentConfig = config;
            return config;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load config: {ex.Message}");
            return new AppConfig(); // Fallback to default
        }
    }

    public static void SaveConfig(AppConfig config)
    {
        try
        {
            if (!Directory.Exists(CONFIG_FOLDER))
            {
                Directory.CreateDirectory(CONFIG_FOLDER);
            }

            string json = JsonSerializer.Serialize(config, _jsonOptions);
            File.WriteAllText(CONFIG_FILE_PATH, json);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save config: {ex.Message}");
        }
    }
}
