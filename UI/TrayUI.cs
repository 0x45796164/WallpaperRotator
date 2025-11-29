using System;
using System.Drawing;
using System.Windows.Forms;
using WallpaperRotator.Core;
using WallpaperRotator.Utilities;

namespace WallpaperRotator.UI;

public class TrayUI : IDisposable
{
    private readonly WallpaperRotatorService _service;
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _contextMenu;

    public TrayUI(WallpaperRotatorService service)
    {
        _service = service;
        _contextMenu = new ContextMenuStrip();
        _notifyIcon = new NotifyIcon();

        InitializeComponent();
    }

    private void InitializeComponent()
    {
        // Context Menu
        _contextMenu.Opening += OnContextMenuOpening;

        var itemSettings = new ToolStripMenuItem("Settings", null, OnSettings);
        var itemRotateAll = new ToolStripMenuItem("Rotate All", null, OnManualRotate);
        
        // Per-monitor rotation submenu
        var itemRotateMonitor = new ToolStripMenuItem("Rotate Display");
        itemRotateMonitor.Name = "RotateMonitor"; // Tag for finding later

        var itemToggleEnable = new ToolStripMenuItem("Disable All", null, OnToggleEnable);
        itemToggleEnable.Name = "ToggleEnable";

        var itemRefresh = new ToolStripMenuItem("Refresh Playlists", null, OnRefreshPlaylists);
        var itemAudio = new ToolStripMenuItem("Mute/Unmute All", null, OnToggleAudio);
        var itemExit = new ToolStripMenuItem("Exit", null, OnExit);

        _contextMenu.Items.Add(itemSettings);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add(itemRotateAll);
        _contextMenu.Items.Add(itemRotateMonitor);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add(itemToggleEnable);
        _contextMenu.Items.Add(itemRefresh);
        _contextMenu.Items.Add(itemAudio);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add(itemExit);

        // Notify Icon
        _notifyIcon.Text = "Wallpaper Rotator";
        _notifyIcon.ContextMenuStrip = _contextMenu;
        _notifyIcon.Visible = true;
        _notifyIcon.MouseDoubleClick += OnSettings; // Double click opens settings

        // Load Icon with dark mode support
        try
        {
            _notifyIcon.Icon = LoadThemeAppropriateIcon();
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load icon: {ex.Message}");
            _notifyIcon.Icon = SystemIcons.Application;
        }

        Logger.Info("Tray UI initialized.");
    }

    private void OnContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Update Toggle Enable Text
        if (_contextMenu.Items["ToggleEnable"] is ToolStripMenuItem itemToggle)
        {
            itemToggle.Text = _service.IsPaused ? "Enable All" : "Disable All";
        }

        // Update Rotate Monitor Submenu
        if (_contextMenu.Items["RotateMonitor"] is ToolStripMenuItem itemRotateMonitor)
        {
            itemRotateMonitor.DropDownItems.Clear();
            foreach (var monitor in _service.Monitors)
            {
                if (monitor.IsEnabled || _service.IsPaused) // Show even if paused? Maybe not.
                {
                    // Use a closure to capture monitor ID
                    string id = monitor.MonitorId;
                    var subItem = new ToolStripMenuItem(monitor.DisplayName, null, (s, args) => _service.ManualRotate(id));
                    itemRotateMonitor.DropDownItems.Add(subItem);
                }
            }
            
            // If no monitors, disable the item
            itemRotateMonitor.Enabled = itemRotateMonitor.DropDownItems.Count > 0;
        }
    }

    private void OnToggleEnable(object? sender, EventArgs e)
    {
        if (_service.IsPaused)
        {
            _service.Resume();
        }
        else
        {
            _service.Pause();
        }
    }

    private Icon LoadThemeAppropriateIcon()
    {
        bool isDarkMode = IsDarkThemeEnabled();
        string iconName = isDarkMode ? "icon-dark.ico" : "icon-light.ico";
        string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, iconName);

        // Try ICO file first (proper format for tray icons)
        if (System.IO.File.Exists(iconPath))
        {
            return new Icon(iconPath, new Size(32, 32));
        }

        // Fallback to PNG if ICO not found
        string pngPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.png");
        if (System.IO.File.Exists(pngPath))
        {
            using (var original = new Bitmap(pngPath))
            using (var resized = new Bitmap(original, new Size(32, 32)))
            {
                return Icon.FromHandle(resized.GetHicon());
            }
        }

        // Final fallback
        return SystemIcons.Application;
    }

    private bool IsDarkThemeEnabled()
    {
        try
        {
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                var value = key?.GetValue("AppsUseLightTheme");
                return value is int i && i == 0; // 0 = dark mode, 1 = light mode
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Could not detect theme, defaulting to light mode: {ex.Message}");
            return false; // Default to light mode if detection fails
        }
    }

    public void ShowNotification(string title, string message)
    {
        _notifyIcon.ShowBalloonTip(5000, title, message, ToolTipIcon.Info);
    }

    public void OpenSettings()
    {
        OnSettings(this, EventArgs.Empty);
    }

    private void OnSettings(object? sender, EventArgs e)
    {
        // Open Settings Window
        var settingsWindow = new SettingsWindow(_service);
        settingsWindow.Show();
    }

    private void OnManualRotate(object? sender, EventArgs e)
    {
        Logger.Info("User requested manual rotation.");
        // Pass null to rotate primary or all, depending on service implementation
        _service.ManualRotate(null); 
    }

    private void OnRefreshPlaylists(object? sender, EventArgs e)
    {
        Logger.Info("User requested playlist refresh.");
        _service.RefreshPlaylists();
    }

    private void OnToggleAudio(object? sender, EventArgs e)
    {
        _service.ToggleGlobalAudio();
    }

    private void OnExit(object? sender, EventArgs e)
    {
        Logger.Info("User requested exit.");
        _notifyIcon.Visible = false; // Hide icon immediately
        _service.Stop();
        Application.Exit();
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _contextMenu.Dispose();
    }
}
