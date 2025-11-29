using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Timers;
using System.Drawing;
using System.Linq;
using System.Windows.Forms; // For Screen
using WallpaperRotator.Core.Models;
using WallpaperRotator.Utilities;
using WallpaperRotator.Windows;
using WallpaperRotator.UI;

namespace WallpaperRotator.Core;

public class RotationEngine : IDisposable
{
    private readonly System.Timers.Timer _timer;
    private readonly MonitorManager _monitorManager;
    private readonly PlaylistManager _playlistManager;
    private readonly object _lock = new();
    
    // Map MonitorId -> WallpaperWindow
    private readonly Dictionary<string, WallpaperWindow> _windows = new();
    private readonly HashSet<string> _pausedMonitors = new();

    public RotationEngine(MonitorManager monitorManager, PlaylistManager playlistManager)
    {
        _monitorManager = monitorManager;
        _playlistManager = playlistManager;

        _timer = new System.Timers.Timer(100); // 100ms interval
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;
    }

    public void Start()
    {
        Logger.Info("RotationEngine starting...");
        ReapplySettings();
        _timer.Start();
    }

    public void Stop()
    {
        Logger.Info("RotationEngine stopping...");
        _timer.Stop();
        CloseAllWindows();
    }

    private void CloseAllWindows()
    {
        foreach (var window in _windows.Values)
        {
            if (window != null) window.Close();
        }
        _windows.Clear();
    }

    private void CreateWindow(string id, Rectangle bounds, MonitorState configSource)
    {
        if (_windows.ContainsKey(id)) return;

        try
        {
            Logger.Info($"Creating window for {id} at {bounds}");
            
            var window = new WallpaperWindow(id, bounds);
            window.Show();
            
            // Subscribe to VideoEnded for smart rotation
            // We use configSource to track state (LastAppliedTime, etc.)
            window.VideoEnded += (s, e) => OnVideoEnded(configSource);
            window.RequestPause += (s, e) => PauseMonitor(configSource.MonitorId);
            window.RequestResume += (s, e) => ResumeMonitor(configSource.MonitorId);
            window.RequestRotate += (s, e) => RotateMonitor(configSource);
            window.RequestPrevious += (s, e) => RotateToPrevious(configSource);



            var handle = window.GetHandle();
            
            // STRATEGY: No Injection (Desktop Overlay/Underlay)
            ApplyToolWindowStyle(handle, bounds);
            
            _windows[id] = window;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to create window for {id}: {ex.Message}");
        }
    }

    private void ApplyToolWindowStyle(IntPtr handle, Rectangle bounds)
    {
        int exStyle = WindowInjector.GetWindowLong(handle, WindowInjector.GWL_EXSTYLE);
        exStyle |= WindowInjector.WS_EX_TOOLWINDOW | WindowInjector.WS_EX_NOACTIVATE;
        WindowInjector.SetWindowLong(handle, WindowInjector.GWL_EXSTYLE, exStyle);

        WindowInjector.SetWindowPos(handle, WindowInjector.HWND_BOTTOM, 
            bounds.X, bounds.Y, bounds.Width, bounds.Height, 
            WindowInjector.SWP_SHOWWINDOW | WindowInjector.SWP_NOACTIVATE);

        Logger.Info($"Created top-level wallpaper window and sent to bottom.");
    }


    private void OnVideoEnded(MonitorState monitor)
    {
        monitor.VideoHasFinished = true;

        if (monitor.RotateOnVideoEnd)
        {
            Logger.Info($"Video ended for {monitor.MonitorId} and RotateOnVideoEnd is true. Rotating.");
            RotateMonitor(monitor);
        }
    }

    public void RotateMonitor(MonitorState monitor, bool preserveCurrent = false, bool addToHistory = true)
    {
        if (monitor == null || !monitor.IsEnabled) return;

        try
        {
            var mode = ConfigManager.CurrentConfig.GlobalSettings.DisplayMode;

            // In Stretch mode, we rotate the "Virtual" monitor
            // In Clone mode, we rotate the "Primary" monitor and apply to all windows
            // In PerDisplay, we rotate the specific monitor

            string? nextWallpaper;
            
            if (preserveCurrent && !string.IsNullOrEmpty(monitor.CurrentWallpaper) && File.Exists(monitor.CurrentWallpaper))
            {
                nextWallpaper = monitor.CurrentWallpaper;
                Logger.Info($"Preserving current wallpaper for {monitor.MonitorId}: {nextWallpaper}");
            }
            else
            {
                nextWallpaper = _playlistManager.GetNext(monitor.PlaylistFolder, monitor.ShuffleMode, monitor.CurrentWallpaper);
            }
            
            if (string.IsNullOrEmpty(nextWallpaper)) return;

            if (!File.Exists(nextWallpaper))
            {
                Logger.Warn($"Wallpaper file missing: {nextWallpaper}");
                return;
            }

            // Determine if video
            string ext = Path.GetExtension(nextWallpaper).ToLower();
            monitor.IsVideo = (ext == ".mp4" || ext == ".webm" || ext == ".avi" || ext == ".mkv" || ext == ".m4v" || ext == ".gif");

            // Apply to Window(s)
            if (mode == DisplayMode.Stretch)
            {
                if (_windows.TryGetValue("VirtualScreen", out var window))
                {
                    bool audio = ConfigManager.CurrentConfig.GlobalSettings.GlobalAudioEnabled && monitor.AudioEnabled;
                    window.SetContent(nextWallpaper, monitor.WallpaperStyle, audio, monitor.BackgroundColor);
                }
            }
            else if (mode == DisplayMode.Clone)
            {
                // Apply to ALL windows
                foreach (var kvp in _windows)
                {
                    var winId = kvp.Key;
                    var win = kvp.Value;

                    // Only play audio on the primary monitor to prevent echo/duplication
                    // The 'monitor' passed here is the primary monitor driving the rotation
                    bool isPrimary = (winId == monitor.MonitorId);
                    
                    bool audio = isPrimary && ConfigManager.CurrentConfig.GlobalSettings.GlobalAudioEnabled && monitor.AudioEnabled;
                    win.SetContent(nextWallpaper, monitor.WallpaperStyle, audio, monitor.BackgroundColor);
                }
            }
            else // PerDisplay
            {
                if (_windows.TryGetValue(monitor.MonitorId, out var window))
                {
                    bool audio = ConfigManager.CurrentConfig.GlobalSettings.GlobalAudioEnabled && monitor.AudioEnabled;
                    window.SetContent(nextWallpaper, monitor.WallpaperStyle, audio, monitor.BackgroundColor);
                }
            }
            
            monitor.CurrentWallpaper = nextWallpaper;
            monitor.LastAppliedTime = DateTime.Now;
            monitor.VideoHasFinished = false; // Reset video state
            
            // Update History
            if (addToHistory)
            {
                AddToHistory(monitor, nextWallpaper);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to rotate {monitor.DisplayName}: {ex.Message}");
        }
    }

    public void RotateToPrevious(MonitorState monitor)
    {
        if (monitor == null || monitor.WallpaperHistory == null || monitor.WallpaperHistory.Count == 0) return;

        lock (_lock)
        {
            if (monitor.WallpaperHistory.Count < 2) return; // Need at least 2 items to go back
            
            // Remove current (last)
            monitor.WallpaperHistory.RemoveAt(monitor.WallpaperHistory.Count - 1);
            
            // Get new last
            string prevWallpaper = monitor.WallpaperHistory.Last();
            
            // Set it, but DO NOT add to history (it's already there)
            SetSpecificWallpaper(monitor, prevWallpaper, false);
        }
    }

    public void SetSpecificWallpaper(MonitorState monitor, string wallpaperPath, bool addToHistory = true)
    {
        if (monitor == null || string.IsNullOrEmpty(wallpaperPath)) return;

        Logger.Info($"Setting specific wallpaper for {monitor.MonitorId}: {wallpaperPath}");
        
        if (!File.Exists(wallpaperPath))
        {
            Logger.Warn($"Specific wallpaper file missing: {wallpaperPath}");
            return;
        }

        // Update state
        monitor.CurrentWallpaper = wallpaperPath;
        monitor.LastAppliedTime = DateTime.Now;
        monitor.VideoHasFinished = false; // Reset video state
        
        // Update History
        if (addToHistory)
        {
            AddToHistory(monitor, wallpaperPath);
        }

        // Determine if video
        string ext = Path.GetExtension(wallpaperPath).ToLower();
        monitor.IsVideo = (ext == ".mp4" || ext == ".webm" || ext == ".avi" || ext == ".mkv" || ext == ".m4v" || ext == ".gif");

        var mode = ConfigManager.CurrentConfig.GlobalSettings.DisplayMode;

        // Display it
        if (mode == DisplayMode.Stretch)
        {
            if (_windows.TryGetValue("VirtualScreen", out var window))
            {
                bool audio = ConfigManager.CurrentConfig.GlobalSettings.GlobalAudioEnabled && monitor.AudioEnabled;
                window.SetContent(wallpaperPath, monitor.WallpaperStyle, audio, monitor.BackgroundColor);
            }
        }
        else if (mode == DisplayMode.Clone)
        {
            foreach (var kvp in _windows)
            {
                var winId = kvp.Key;
                var win = kvp.Value;
                bool isPrimary = (winId == monitor.MonitorId);
                bool audio = isPrimary && ConfigManager.CurrentConfig.GlobalSettings.GlobalAudioEnabled && monitor.AudioEnabled;
                win.SetContent(wallpaperPath, monitor.WallpaperStyle, audio, monitor.BackgroundColor);
            }
        }
        else // PerDisplay
        {
            if (_windows.TryGetValue(monitor.MonitorId, out var window))
            {
                bool audio = ConfigManager.CurrentConfig.GlobalSettings.GlobalAudioEnabled && monitor.AudioEnabled;
                window.SetContent(wallpaperPath, monitor.WallpaperStyle, audio, monitor.BackgroundColor);
            }
        }
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (!System.Threading.Monitor.TryEnter(_lock)) return;

        try
        {
            var mode = ConfigManager.CurrentConfig.GlobalSettings.DisplayMode;

            if (mode == DisplayMode.PerDisplay)
            {
                foreach (var monitor in _monitorManager.Monitors)
                {
                    if (!monitor.IsEnabled) continue;
                    CheckAndRotate(monitor);
                }
            }
            else
            {
                // Stretch or Clone: Drive from PRIMARY monitor config
                var primary = _monitorManager.Monitors.FirstOrDefault();
                if (primary != null && primary.IsEnabled)
                {
                    CheckAndRotate(primary);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error in rotation loop: {ex.Message}");
        }
        finally
        {
            System.Threading.Monitor.Exit(_lock);
        }
    }

    private void CheckAndRotate(MonitorState monitor)
    {
        if (_pausedMonitors.Contains(monitor.MonitorId)) return;

        var elapsed = DateTime.Now - monitor.LastAppliedTime;
        
        // If interval is 0 or less, do not rotate automatically
        if (monitor.RotationIntervalMs <= 0) return;

        if (elapsed.TotalMilliseconds >= monitor.RotationIntervalMs)
        {
            if (monitor.IsVideo)
            {
                // Video Logic
                if (monitor.RotateOnVideoEnd)
                {
                    // Do nothing, wait for OnVideoEnded
                }
                else
                {
                    // Looping behavior
                    if (monitor.BypassIntervalForLongVideo)
                    {
                        // Only rotate if video has finished at least once
                        if (monitor.VideoHasFinished)
                        {
                            RotateMonitor(monitor);
                        }
                    }
                    else
                    {
                        // Strict interval, cut off video
                        RotateMonitor(monitor);
                    }
                }
            }
            else
            {
                RotateMonitor(monitor);
            }
        }
    }

    public void UpdateAudioSettings()
    {
        ReapplySettings(true);
    }

    public void ReapplySettings(bool preserveCurrent = false)
    {
        // Close existing windows to reset state (simplest way to handle mode switch)
        // Optimization: Could be smarter, but mode switch is rare.
        CloseAllWindows();

        var mode = ConfigManager.CurrentConfig.GlobalSettings.DisplayMode;

        if (mode == DisplayMode.Stretch)
        {
            // Create ONE window covering virtual screen
            var virtualBounds = SystemInformation.VirtualScreen;
            var primary = _monitorManager.Monitors.FirstOrDefault();
            
            if (primary != null && primary.IsEnabled)
            {
                CreateWindow("VirtualScreen", virtualBounds, primary);
                RotateMonitor(primary, preserveCurrent);
            }
        }
        else if (mode == DisplayMode.Clone)
        {
            // Create windows for ALL monitors, but driven by PRIMARY config
            var primary = _monitorManager.Monitors.FirstOrDefault();
            
            if (primary != null && primary.IsEnabled)
            {
                foreach (var monitor in _monitorManager.Monitors)
                {
                    // Use monitor's own bounds, but primary's config
                    var bounds = new Rectangle(
                        monitor.WorkArea.Left, monitor.WorkArea.Top, 
                        monitor.WorkArea.Right - monitor.WorkArea.Left, 
                        monitor.WorkArea.Bottom - monitor.WorkArea.Top);
                        
                    CreateWindow(monitor.MonitorId, bounds, primary);
                }
                RotateMonitor(primary, preserveCurrent);
            }
        }
        else // PerDisplay
        {
            foreach (var monitor in _monitorManager.Monitors)
            {
                if (monitor.IsEnabled)
                {
                    var bounds = new Rectangle(
                        monitor.WorkArea.Left, monitor.WorkArea.Top, 
                        monitor.WorkArea.Right - monitor.WorkArea.Left, 
                        monitor.WorkArea.Bottom - monitor.WorkArea.Top);
                        
                    CreateWindow(monitor.MonitorId, bounds, monitor);
                    RotateMonitor(monitor, preserveCurrent);
                }
            }
        }
    }

    private void AddToHistory(MonitorState monitor, string wallpaperPath)
    {
        if (monitor.WallpaperHistory == null) monitor.WallpaperHistory = new List<string>();
        
        // Avoid duplicates at the end
        if (monitor.WallpaperHistory.Count > 0 && monitor.WallpaperHistory.Last() == wallpaperPath) return;

        monitor.WallpaperHistory.Add(wallpaperPath);
        
        if (monitor.WallpaperHistory.Count > 5)
        {
            monitor.WallpaperHistory.RemoveAt(0);
        }
    }

    public void PauseMonitor(string monitorId)
    {
        lock (_lock)
        {
            _pausedMonitors.Add(monitorId);
        }
    }

    public void ResumeMonitor(string monitorId)
    {
        lock (_lock)
        {
            _pausedMonitors.Remove(monitorId);
        }
    }

    public void Dispose()
    {
        Stop();
        _timer.Dispose();
    }
}
