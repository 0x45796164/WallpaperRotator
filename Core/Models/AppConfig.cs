using System.Collections.Generic;

namespace WallpaperRotator.Core.Models;

public class AppConfig
{
    public List<MonitorState> Monitors { get; set; } = new();
    public GlobalSettings GlobalSettings { get; set; } = new();
}

public class GlobalSettings
{
    public bool StartWithWindows { get; set; } = true;
    public bool MinimizeToTray { get; set; } = true;
    public bool DebugLogging { get; set; } = false;
    public bool GlobalAudioEnabled { get; set; } = true;
    public DisplayMode DisplayMode { get; set; } = DisplayMode.PerDisplay;
}

public enum DisplayMode
{
    PerDisplay,
    Stretch,
    Clone
}

public enum ShuffleMode
{
    Random,
    SortedByName,
    SortedByDate
}
