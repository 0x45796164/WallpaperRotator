using System;
using WallpaperRotator.Utilities;
using WallpaperRotator.Core.Models;

namespace WallpaperRotator.Core;

public class WallpaperRotatorService
{
    private AppConfig _config = new();
    private MonitorManager _monitorManager = new();
    private PlaylistManager _playlistManager = new();
    private RotationEngine? _rotationEngine;
    private WallpaperRotator.UI.TrayUI? _trayUI;

    public void Start()
    {
        Logger.Info("WallpaperRotatorService starting...");
        
        try
        {
            // 1. Load Config
            _config = ConfigManager.LoadConfig();
            Logger.Info($"Config loaded. Global settings: StartWithWindows={_config.GlobalSettings.StartWithWindows}");

            // 2. Initialize Monitors
            _monitorManager.Initialize(_config);
            
            // 3. Initialize Rotation Engine
            _rotationEngine = new RotationEngine(_monitorManager, _playlistManager);
            _rotationEngine.Start();

            // 4. Initialize UI (if configured)
            if (_config.GlobalSettings.MinimizeToTray)
            {
                _trayUI = new WallpaperRotator.UI.TrayUI(this);
            }

            // 5. Save config back (in case of new monitors)
            ConfigManager.SaveConfig(_config);

            // 6. First Run / Unconfigured Check
            bool needsConfig = _monitorManager.Monitors.All(m => string.IsNullOrEmpty(m.PlaylistFolder));
            if (needsConfig)
            {
                var result = System.Windows.Forms.MessageBox.Show(
                    "Welcome to Wallpaper Rotator!\n\nIt looks like you haven't set up any wallpaper folders yet.\nWould you like to open Settings now to configure your monitors?",
                    "First Run Setup",
                    System.Windows.Forms.MessageBoxButtons.YesNo,
                    System.Windows.Forms.MessageBoxIcon.Information
                );

                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    _trayUI?.OpenSettings();
                }
            }
            else
            {
                // 7. Check if any monitor is enabled
                bool anyEnabled = _monitorManager.Monitors.Any(m => m.IsEnabled);
                if (!anyEnabled)
                {
                    _trayUI?.ShowNotification(
                        "No Displays Enabled", 
                        "Wallpaper rotation is currently disabled. Open Settings to enable a monitor."
                    );
                }
            }

            Logger.Info("WallpaperRotatorService started successfully.");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to start service: {ex}");
            throw;
        }
    }

    public void Stop()
    {
        Logger.Info("WallpaperRotatorService stopping...");
        _trayUI?.Dispose();
        _rotationEngine?.Stop();
        _rotationEngine?.Dispose();
        _playlistManager.Dispose();
        ConfigManager.SaveConfig(_config);
        Logger.Info("WallpaperRotatorService stopped.");
    }

    public void ManualRotate(string? monitorId)
    {
        // If monitorId is null, rotate all or primary. For now, let's rotate all active monitors.
        if (_rotationEngine == null) return;

        foreach (var monitor in _monitorManager.Monitors)
        {
            if (monitorId == null || monitor.MonitorId == monitorId)
            {
                _rotationEngine.RotateMonitor(monitor);
            }
        }
    }

    public void SetSpecificWallpaper(string monitorId, string wallpaperPath)
    {
        if (_rotationEngine == null) return;
        
        var monitor = _monitorManager.Monitors.FirstOrDefault(m => m.MonitorId == monitorId);
        if (monitor != null)
        {
            _rotationEngine.SetSpecificWallpaper(monitor, wallpaperPath);
        }
    }

    public void RefreshPlaylists()
    {
        foreach (var monitor in _monitorManager.Monitors)
        {
            if (!string.IsNullOrEmpty(monitor.PlaylistFolder))
            {
                _playlistManager.RefreshPlaylist(monitor.PlaylistFolder);
            }
        }
    }

    public void ToggleGlobalAudio()
    {
        _config.GlobalSettings.GlobalAudioEnabled = !_config.GlobalSettings.GlobalAudioEnabled;
        ConfigManager.SaveConfig(_config);
        _rotationEngine?.UpdateAudioSettings();
    }

    public void UpdateAudioSettings()
    {
        _rotationEngine?.UpdateAudioSettings();
    }

    public void ReapplySettings(bool preserveCurrent = false)
    {
        _rotationEngine?.ReapplySettings(preserveCurrent);
    }

    public void SetStartWithWindows(bool enable)
    {
        try
        {
            string runKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(runKey, true))
            {
                if (enable)
                {
                    string? exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                    if (exePath != null)
                    {
                        key?.SetValue("WallpaperRotator", $"\"{exePath}\"");
                    }
                }
                else
                {
                    key?.DeleteValue("WallpaperRotator", false);
                }
            }
            _config.GlobalSettings.StartWithWindows = enable;
            ConfigManager.SaveConfig(_config);
        }
        catch (Exception ex)
        {
            Utilities.Logger.Error($"Failed to set StartWithWindows: {ex.Message}");
        }
    }
}
