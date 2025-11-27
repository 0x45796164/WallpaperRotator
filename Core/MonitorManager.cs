using System;
using System.Collections.Generic;
using System.Linq;
using WallpaperRotator.Core.Models;
using WallpaperRotator.Utilities;
using WallpaperRotator.Windows;

namespace WallpaperRotator.Core;

public class MonitorManager
{
    private readonly Dictionary<string, MonitorState> _monitors = new();
    
    public IReadOnlyList<MonitorState> Monitors => _monitors.Values.ToList();

    public void Initialize(AppConfig config)
    {
        Logger.Info("Initializing MonitorManager...");
        RefreshMonitors(config);
    }

    public void RefreshMonitors(AppConfig config)
    {
        try
        {
            var detectedMonitors = WindowsInterop.GetMonitors();
            Logger.Info($"Detected {detectedMonitors.Count} monitors.");

            var activeMonitorIds = new HashSet<string>();

            foreach (var nativeMonitor in detectedMonitors)
            {
                string deviceName = nativeMonitor.szDevice;
                activeMonitorIds.Add(deviceName);

                if (!_monitors.ContainsKey(deviceName))
                {
                    // Check if we have config for this monitor
                    var existingConfig = config.Monitors.FirstOrDefault(m => m.MonitorId == deviceName);
                    
                    if (existingConfig != null)
                    {
                        _monitors[deviceName] = existingConfig;
                        Logger.Info($"Loaded config for monitor: {deviceName}");
                    }
                    else
                    {
                        // New monitor discovery
                        var newMonitor = new MonitorState
                        {
                            MonitorId = deviceName,
                            DisplayName = deviceName,
                            IsEnabled = false,
                            Bounds = nativeMonitor.rcMonitor,
                            WorkArea = nativeMonitor.rcWork,
                            PlaylistFolder = null // User will configure their own folder
                        };
                        
                        _monitors[deviceName] = newMonitor;
                        config.Monitors.Add(newMonitor);
                        Logger.Info($"Discovered new monitor: {deviceName}. Playlist folder not set - user needs to configure.");
                    }
                    
                    // Always update bounds in case they changed (resolution change)
                    _monitors[deviceName].Bounds = nativeMonitor.rcMonitor;
                    _monitors[deviceName].WorkArea = nativeMonitor.rcWork;
                }
            }

            // Optional: Mark monitors as inactive if they are no longer detected?
            // For now, we just keep them in config but they won't be in the active loop if we iterate over detected ones.
            // Or we can just rely on _monitors containing everything.
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to refresh monitors: {ex.Message}");
        }
    }
}
