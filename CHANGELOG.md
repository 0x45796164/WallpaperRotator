# Changelog

All notable changes to Wallpaper Rotator will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-11-27

### Added
- Multi-monitor support with independent configuration per display
- Image wallpaper support (JPG, PNG, BMP, WebP, GIF)
- Video wallpaper support (MP4, MKV, AVI, MOV, M4V, WEBM)
- Intuitive time picker UI for rotation intervals (hours, minutes, seconds)
- Per-monitor wallpaper style selection (Fill, Fit, Stretch, Center, Cover)
- Custom background colors for non-filling content
- Global and per-monitor audio control for videos
- System tray integration with quick actions menu
- Windows 11 Mica effect for modern aesthetics
- Auto-start with Windows option
- Manual rotation controls per monitor
- Smart video looping (plays full video or loops until interval)
- Taskbar preservation (wallpapers exclude taskbar area)
- Automatic monitor detection and configuration
- Configuration persistence via JSON
- **Shuffle Modes**: Random, By Name, By Date (Last Modified)
- **History & Navigation**: View recent wallpapers and locate them in Explorer
- **Video Options**: "Rotate on Video End" and "Allow Long Video to Finish"
- **Save Behavior**: "Save Changes" now preserves the current wallpaper
- **Display Modes**: Per Display, Stretch (Span), Clone (Mirror)
- **Portable Versions**: Full (Self-Contained) and Lite (Framework-Dependent) options
- **Fresh Install Experience**: Auto-enables monitors on interaction; "No Enabled Display" reminder

### Features
- **Modern Interface**: Windows 11-style UI with Mica material, dark theme, rounded corners
- **Visual Monitor Cards**: Easy-to-understand overview of all connected displays
- **Toggle Switches**: Simple on/off controls for features
- **Tray Menu**: Quick access to settings, rotation, and audio controls
- **No Injection Strategy**: Safe, non-intrusive window layering compatible with Windows 11 24H2
- **Low Resource Usage**: Efficient memory and CPU footprint

### Technical
- Built on .NET 7.0 Desktop Runtime
- WPF-based UI with Windows Forms interop for wallpaper windows
- P/Invoke for Windows API integration (monitor detection, window positioning)
- JSON configuration storage in %APPDATA%
- Comprehensive error logging to debug.log

### Known Limitations
- Desktop icons remain visible above wallpaper (by design)
- Right-click on wallpaper does not show desktop context menu (Z-order constraint)
- Requires .NET 7.0 Desktop Runtime
- Video format support depends on installed codecs (K-Lite Codec Pack recommended)

---

## Future Considerations

Potential features for future releases:
- Playlist management (create, edit, delete custom playlists)
- Slideshow transitions and effects
- Web-based wallpapers (embedded browser)
- Interactive wallpaper controls (pause/play buttons)
- Wallpaper scheduling (different wallpapers at different times)
- Theme support for UI customization

---

**Note**: Version 1.0.0 represents the initial public release. This software was developed with AI assistance.
