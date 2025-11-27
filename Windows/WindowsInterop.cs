using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.IO;

namespace WallpaperRotator.Windows;

public static class WindowsInterop
{
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool SystemParametersInfo(
        uint action,
        uint uParam,
        string lpvParam,
        uint winIni
    );

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    private delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    public static void ApplyWallpaper(string wallpaperPath)
    {
        if (string.IsNullOrWhiteSpace(wallpaperPath))
        {
            throw new ArgumentException("Wallpaper path cannot be empty", nameof(wallpaperPath));
        }
        
        if (!File.Exists(wallpaperPath))
        {
            throw new FileNotFoundException($"Wallpaper file not found: {wallpaperPath}");
        }
        
        bool result = SystemParametersInfo(
            NativeConstants.SPI_SETDESKWALLPAPER,
            0,
            wallpaperPath,
            NativeConstants.SPIF_UPDATEINIFILE | NativeConstants.SPIF_SENDWININICHANGE
        );
        
        if (!result)
        {
            int error = Marshal.GetLastWin32Error();
            throw new Win32Exception(error, $"Failed to apply wallpaper: error {error}");
        }
    }

    public static List<MONITORINFOEX> GetMonitors()
    {
        var monitors = new List<MONITORINFOEX>();

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
            delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
            {
                MONITORINFOEX mi = new MONITORINFOEX();
                mi.cbSize = (uint)Marshal.SizeOf(mi);
                
                if (GetMonitorInfo(hMonitor, ref mi))
                {
                    monitors.Add(mi);
                }
                return true;
            },
            IntPtr.Zero);

        return monitors;
    }
}
