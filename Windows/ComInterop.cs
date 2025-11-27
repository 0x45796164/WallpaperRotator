using System;
using System.Runtime.InteropServices;

namespace WallpaperRotator.Windows;

[ComImport]
[Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IDesktopWallpaper
{
    void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID, [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);
    
    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID);

    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetMonitorDevicePathAt(uint monitorIndex);

    [return: MarshalAs(UnmanagedType.U4)]
    uint GetMonitorDevicePathCount();

    [return: MarshalAs(UnmanagedType.Struct)]
    RECT GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitorID);

    void SetBackgroundColor([MarshalAs(UnmanagedType.U4)] uint color);

    [return: MarshalAs(UnmanagedType.U4)]
    uint GetBackgroundColor();

    void SetPosition([MarshalAs(UnmanagedType.I4)] int position);

    [return: MarshalAs(UnmanagedType.I4)]
    int GetPosition();

    void SetSlideshow(IntPtr items);
    IntPtr GetSlideshow();

    void AdvanceSlideshow([MarshalAs(UnmanagedType.LPWStr)] string monitorID, [MarshalAs(UnmanagedType.I4)] int direction);

    [return: MarshalAs(UnmanagedType.U4)]
    uint GetStatus();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool Enable([MarshalAs(UnmanagedType.Bool)] bool enable);
}

[ComImport]
[Guid("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD")]
public class DesktopWallpaper
{
}
