# Troubleshooting Guide

> [!NOTE]
> **AI-Generated Code**: This software was developed with AI assistance. If you encounter unusual behavior, please verify your configuration and check the logs.

## Common Issues

### 1. Wallpaper is Invisible (Audio plays, but no video/image)
**Symptoms:** You hear the video audio, but the desktop background remains unchanged or black.
**Cause:** On Windows 11 (especially version 24H2), the "Active Wallpaper" injection technique (WorkerW) can fail because the desktop icons (`SHELLDLL_DefView`) remain attached to the main desktop window (`Progman`), which paints over any child windows.
**Solution:**
The application uses a **"No Injection" Strategy** by default:
# Troubleshooting Guide

This document covers common issues and their solutions for Wallpaper Rotator.

## Installation Issues

### .NET Runtime Not Found

**Symptom**: Application fails to start with error about missing .NET Runtime.

**Solution**:
1. Download and install [.NET 7.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/7.0)
2. Choose "Desktop Runtime" (not SDK or ASP.NET Core Runtime)
3. Select x64 version for 64-bit Windows
4. Restart the application after installation

### Application Won't Start

**Solution**:
1. Check Windows Event Viewer for error details (Windows Logs → Application)
2. Verify .NET 7.0 Desktop Runtime is installed
3. Try running as Administrator (right-click → Run as Administrator)
4. Check antivirus isn't blocking the application

### "File in Use" Error During Update

**Symptom**: Installer fails saying `WallpaperRotator.exe` or `WallpaperRotator.dll` is in use.

**Solution**:
1. Ensure the application is fully closed (check System Tray).
2. Open Task Manager and kill any `WallpaperRotator.exe` processes.
3. If the issue persists, restart your computer and try installing again.

### Portable Lite Version Won't Start

**Symptom**: `WallpaperRotator-Lite.exe` fails to open or shows an error about missing .NET Runtime.

**Solution**:
1. The Lite version requires .NET 7.0 Desktop Runtime.
2. Download and install it from [Microsoft's website](https://dotnet.microsoft.com/download/dotnet/7.0).
3. Alternatively, use `WallpaperRotator-Portable.exe` which includes .NET built-in.

---

## Monitor Detection Issues

### Monitor Not Detected

**Symptom**: Connected monitor doesn't appear in the monitor list.

**Solution**:
1. Ensure monitor is connected and active in Windows Display Settings
2. Click "Refresh Playlists" in tray menu to force re-detection
3. Restart the application
4. Check `debug.log` for monitor enumeration errors

### Monitor Settings Not Saving

**Symptom**: Changes to monitor configuration are not persisted.

**Solution**:
1. Verify you clicked "Save Changes" button (not just closing the window)
2. Check if `config.json` exists in `%APPDATA%\WallpaperRotator\`
3. Ensure the application has write permissions to AppData folder
4. Try running as Administrator if permission issues persist

---

## Wallpaper Display Issues

### Wallpapers Not Rotating

**Symptom**: Wallpapers remain static, no automatic rotation.

**Solution**:
1. Verify monitor is **Enabled** (toggle switch ON)
2. Check that playlist folder is set and contains valid images/videos
3. Ensure rotation interval is greater than 0
4. Check "Enable Rotation" in System Settings
5. Look for errors in `debug.log`

### Taskbar Covered by Wallpaper

**Symptom**: Wallpaper hides the Windows taskbar.

**Status**: This issue has been fixed in the current version. The application now uses work area bounds to exclude the taskbar.

**If still occurring**:
1. Restart the application
2. Update to the latest version
3. Check for Windows 11 24H2-specific issues in release notes

### Video Wallpapers Not Playing

**Symptom**: Videos don't display or show as black screen.

**Solution**:
1. Install [K-Lite Codec Pack](https://codecguide.com/download_kl.htm) (Basic version is sufficient)
2. Ensure video files are in a supported format (MP4, MKV, AVI, MOV)
3. Try a different video file to rule out file corruption
4. Check if video plays in Windows Media Player
5. Review `debug.log` for codec errors

### Images Not Displaying

**Symptom**: Image files don't show as wallpaper.

**Solution**:
1. Verify files are in supported formats (JPG, PNG, BMP, WebP, GIF)
2. Check file isn't corrupted (open in Windows Photos app)
3. Ensure playlist folder path is correct
4. Try refreshing playlists from tray menu
5. Check `debug.log` for file loading errors

---

## Audio Issues

### No Audio from Video Wallpapers

**Symptom**: Videos play but no sound is heard.

**Solution**:
1. Verify "Mute All" is not enabled in tray menu
2. Check per-monitor audio toggle is ON in settings
3. Ensure Windows volume is not muted
4. Install K-Lite Codec Pack if not already installed
5. Check if video has an audio track (some videos are silent)

### Audio Crackling or Distortion

**Symptom**: Video audio plays with noise or distortion.

**Solution**:
1. Update audio drivers
2. Try different video files to isolate issue
3. Reduce simultaneous video playback (disable some monitors)
4. Check Windows audio settings for sample rate mismatches

---

## Performance Issues

### High CPU Usage

**Symptom**: Application uses excessive CPU resources.

**Solution**:
1. Reduce number of video wallpapers playing simultaneously
2. Use images instead of videos on some monitors
3. Increase rotation intervals to reduce file I/O
4. Close other resource-intensive applications
5. Consider using lower resolution videos

### High Memory Usage

**Symptom**: Application consumes large amounts of RAM.

**Solution**:
1. Reduce playlist folder sizes (remove unused files)
2. Use compressed image formats (WebP instead of PNG)
3. Avoid very high resolution wallpapers (e.g., 8K+)
4. Restart application periodically to clear cache

---

## Configuration Issues

### Settings Window Won't Open

**Symptom**: Double-clicking tray icon doesn't show settings window.

**Solution**:
1. Check if window is hidden behind other windows (Alt+Tab)
2. Try right-click → Settings instead
3. Restart the application
4. Check Task Manager if multiple instances are running

### Time Picker Not Changing Values

**Symptom**: Cannot adjust hours/minutes/seconds with up/down buttons.

**Solution**:
1. Ensure you're clicking directly on the ▲▼ buttons
2. Try clicking multiple times (values increment by 1 for hours, 5 for minutes/seconds)
3. Restart the application if buttons are unresponsive
4. Check `debug.log` for UI errors

---

## Advanced Troubleshooting

### Viewing Debug Logs

Logs are located at: `%APPDATA%\WallpaperRotator\debug.log`

To open:
1. Press `Win + R`
2. Type: `%APPDATA%\WallpaperRotator`
3. Open `debug.log` in Notepad

Look for ERROR or WARNING messages for clues about issues.

### Resetting Configuration

If configuration is corrupted:
1. Close application
2. Navigate to `%APPDATA%\WallpaperRotator\`
3. Rename `config.json` to `config.json.backup`
4. Restart application (creates fresh config)

### Clean Reinstall

For persistent issues:
1. Close application
2. Uninstall via Windows Settings
3. Delete `%APPDATA%\WallpaperRotator\` folder
4. Reinstall from latest release
5. Reconfigure settings

---

## Known Limitations

### Desktop Icons Hidden
Desktop icons are positioned above the wallpaper layer and remain visible. If icons appear hidden, this is a Windows display setting issue, not related to this application.

### Right-Click Context Menu
Right-clicking the wallpaper does not show the Windows desktop context menu. This is an intentional design choice due to Z-order constraints. Use `Win + D` to access the desktop directly.

### Windows 11 24H2 Compatibility
The application uses a "No Injection" strategy that's fully compatible with Windows 11 24H2. No additional workarounds needed.

---

## Getting Help

If your issue isn't listed here:
1. Check the [Issues page on GitHub](#) for similar problems
2. Review `debug.log` for error messages
3. Create a new issue with:
   - Windows version
   - Application version
   - Steps to reproduce
   - Relevant log excerpts

---

## Technical Notes

### Window Strategy
The application uses **HWND_BOTTOM** positioning instead of Progman/WorkerW injection. This is safer and more compatible with Windows 11 updates but means the wallpaper appears behind desktop icons.

### Video Playback
Videos are played using WPF's MediaElement, which relies on Windows Media Foundation codecs. For best compatibility, install K-Lite Codec Pack.

### Mica Effect
The settings window uses Windows 11's Mica material effect. On Windows 10, it falls back to a solid dark background. Use at your own risk.
