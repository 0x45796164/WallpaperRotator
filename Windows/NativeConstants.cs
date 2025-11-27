namespace WallpaperRotator.Windows;

internal static class NativeConstants
{
    internal const uint SPI_SETDESKWALLPAPER = 20;
    internal const uint SPIF_UPDATEINIFILE = 0x01;
    internal const uint SPIF_SENDWININICHANGE = 0x02;
    
    internal const int MONITOR_DEFAULTTOPRIMARY = 0x00000001;
    internal const int MONITOR_DEFAULTTONEAREST = 0x00000002;
}
