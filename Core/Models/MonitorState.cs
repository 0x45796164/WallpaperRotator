using System;
using WallpaperRotator.Windows;

namespace WallpaperRotator.Core.Models;

public class MonitorState
{
    public string MonitorId { get; set; } = string.Empty; // \\.\DISPLAY1
    public string DisplayName { get; set; } = string.Empty;
    public int RotationIntervalMs { get; set; } = 30000; // Default 30 seconds
    public string? PlaylistFolder { get; set; }
    public bool IsEnabled { get; set; } = false;

    // New Options
    public bool AudioEnabled { get; set; } = true;
    public string WallpaperStyle { get; set; } = "Fill"; // Fill, Fit, Center, Stretch
    public string BackgroundColor { get; set; } = "#000000"; // Hex color
    public ShuffleMode ShuffleMode { get; set; } = ShuffleMode.Random;
    public bool RotateOnVideoEnd { get; set; } = false;
    public bool BypassIntervalForLongVideo { get; set; } = true;

    // Runtime state
    public string? CurrentWallpaper { get; set; }
    public List<string> WallpaperHistory { get; set; } = new();
    public DateTime LastAppliedTime { get; set; }
    
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsVideo { get; set; } = false;

    [System.Text.Json.Serialization.JsonIgnore]
    public bool VideoHasFinished { get; set; } = false;
    
    // For COM matching
    public RECT Bounds { get; set; } // Full monitor bounds (rcMonitor)
    public RECT WorkArea { get; set; } // Work area excluding taskbar (rcWork)
    public string? ComDeviceId { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public string ResolutionDescription => $"{Bounds.Right - Bounds.Left}x{Bounds.Bottom - Bounds.Top} @ ({Bounds.Left},{Bounds.Top})";
}
