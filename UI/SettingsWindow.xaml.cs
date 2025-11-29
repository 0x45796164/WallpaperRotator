using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms; // For FolderBrowserDialog, ColorDialog, OpenFileDialog
using System.Windows.Interop;
using System.Windows.Media; // For Brushes, Color
using WallpaperRotator.Core;
using WallpaperRotator.Core.Models;
using WallpaperRotator.Utilities;

namespace WallpaperRotator.UI
{
    public partial class SettingsWindow : Window
    {
        private readonly WallpaperRotatorService _service;
        private AppConfig _config;
        private MonitorState? _selectedMonitor;

        public SettingsWindow(WallpaperRotatorService service)
        {
            InitializeComponent();
            _service = service;
            _config = ConfigManager.CurrentConfig;

            // Set Icon
            try
            {
                // Try ICO first
                string iconName = "icon-light.ico"; // Default
                string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, iconName);
                
                if (!System.IO.File.Exists(iconPath))
                {
                    // Fallback to PNG
                    iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.png");
                }

                if (System.IO.File.Exists(iconPath))
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage(new Uri(iconPath));
                    this.Icon = bitmap;
                    AppIcon.Source = bitmap;
                }
            }
            catch { }

            LoadData();
            
            // Enable Mica
            this.SourceInitialized += (s, e) => EnableMica();
            
            AdjustWindowSize();
        }

        private void AdjustWindowSize()
        {
            try
            {
                var workArea = System.Windows.SystemParameters.WorkArea;
                
                // Ideal size
                double targetWidth = 1200;
                double targetHeight = 800;

                // Constrain to 90% of screen size to ensure it fits
                if (targetWidth > workArea.Width * 0.9) targetWidth = workArea.Width * 0.9;
                if (targetHeight > workArea.Height * 0.9) targetHeight = workArea.Height * 0.9;

                // Ensure minimums
                this.Width = Math.Max(800, targetWidth);
                this.Height = Math.Max(600, targetHeight);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to adjust window size: {ex.Message}");
            }
        }

        // --- Window Chrome Events ---
        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void BtnMaximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        // --- Mica Implementation ---
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_MICA_EFFECT = 1029;

        private void EnableMica()
        {
            try
            {
                var helper = new WindowInteropHelper(this);
                IntPtr hwnd = helper.Handle;

                int trueValue = 1;
                // Enable Dark Mode
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref trueValue, sizeof(int));
                
                // Enable Mica (Windows 11 only)
                // Note: This might fail on Win10, but we catch exception.
                DwmSetWindowAttribute(hwnd, DWMWA_MICA_EFFECT, ref trueValue, sizeof(int));
                
                // Set background to transparent to let Mica show through
                this.Background = System.Windows.Media.Brushes.Transparent;
            }
            catch (Exception ex)
            {
                // Fallback to dark background if Mica fails
                this.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#202020"));
                Logger.Error($"Failed to enable Mica: {ex.Message}");
            }
        }

        // --- Data Loading & Navigation ---
        private void LoadData()
        {
            // Global
            ChkGlobalAudio.IsChecked = _config.GlobalSettings.GlobalAudioEnabled;
            ChkStartWindows.IsChecked = _config.GlobalSettings.StartWithWindows;
            ChkMinimizeTray.IsChecked = _config.GlobalSettings.MinimizeToTray;
            ChkDebugLog.IsChecked = _config.GlobalSettings.DebugLogging;
            ChkDoubleClickRotate.IsChecked = _config.GlobalSettings.EnableDoubleClickRotate;
            ChkHoverActions.IsChecked = _config.GlobalSettings.EnableHoverActions;
            ChkDeleteNoConfirm.IsChecked = _config.GlobalSettings.DeleteWithoutConfirmation;
            CmbDisplayMode.SelectedIndex = (int)_config.GlobalSettings.DisplayMode;

            // Monitors
            RefreshMonitorList();

            // Logs
            RefreshLogs();
        }

        private void RefreshMonitorList()
        {
            MonitorList.ItemsSource = null;

            if (_config.GlobalSettings.DisplayMode == DisplayMode.PerDisplay)
            {
                MonitorList.ItemsSource = _config.Monitors;
            }
            else
            {
                // In Stretch or Clone mode, we only show ONE "virtual" monitor config
                // We'll use the first monitor as the "primary" config holder
                if (_config.Monitors.Count > 0)
                {
                    var primary = _config.Monitors.FirstOrDefault();
                    if (primary != null)
                    {
                        // Create a temporary display wrapper or just use the primary one but change display name
                        // For simplicity, let's just show the primary one but we might want to rename it in UI
                        // But MonitorState is bound directly.
                        MonitorList.ItemsSource = new List<MonitorState> { primary };
                    }
                }
            }

            // Select first monitor if available
            if (MonitorList.Items.Count > 0)
            {
                MonitorList.SelectedIndex = 0;
            }
        }

        private void Nav_Checked(object sender, RoutedEventArgs e)
        {
            if (ViewMonitors == null) return;

            ViewMonitors.Visibility = Visibility.Collapsed;
            ViewGlobal.Visibility = Visibility.Collapsed;
            ViewLogs.Visibility = Visibility.Collapsed;

            if (NavMonitors.IsChecked == true) ViewMonitors.Visibility = Visibility.Visible;
            if (NavGlobal.IsChecked == true) ViewGlobal.Visibility = Visibility.Visible;
            if (NavLogs.IsChecked == true) ViewLogs.Visibility = Visibility.Visible;
        }

        // --- Monitor Logic ---
        private void MonitorList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedMonitor = MonitorList.SelectedItem as MonitorState;
            if (_selectedMonitor != null)
            {
                MonitorDetailsPanel.Visibility = Visibility.Visible;
                
                ChkEnabled.IsChecked = _selectedMonitor.IsEnabled;
                ChkAudio.IsChecked = _selectedMonitor.AudioEnabled;
                TxtPlaylist.Text = _selectedMonitor.PlaylistFolder ?? "";
                
                // Convert milliseconds to hours, minutes, seconds
                int totalSeconds = _selectedMonitor.RotationIntervalMs / 1000;
                int hours = totalSeconds / 3600;
                int minutes = (totalSeconds % 3600) / 60;
                int seconds = totalSeconds % 60;
                
                TxtHours.Text = hours.ToString();
                TxtMinutes.Text = minutes.ToString();
                TxtSeconds.Text = seconds.ToString();
                
                TxtColor.Text = _selectedMonitor.BackgroundColor ?? "#000000";
                UpdateColorPreview(TxtColor.Text);

                // Style
                foreach (ComboBoxItem item in CmbStyle.Items)
                {
                    if (item.Content.ToString()?.ToLower() == (_selectedMonitor.WallpaperStyle ?? "fill").ToLower())
                    {
                        CmbStyle.SelectedItem = item;
                        break;
                    }
                }

                // Shuffle Mode
                CmbShuffleMode.SelectedIndex = (int)_selectedMonitor.ShuffleMode;

                // Video Options
                ChkRotateOnVideoEnd.IsChecked = _selectedMonitor.RotateOnVideoEnd;
                ChkBypassInterval.IsChecked = _selectedMonitor.BypassIntervalForLongVideo;

                // History
                // Show reversed history (newest first)
                if (_selectedMonitor.WallpaperHistory != null)
                {
                    ListHistory.ItemsSource = _selectedMonitor.WallpaperHistory.AsEnumerable().Reverse().ToList();
                }
                else
                {
                    ListHistory.ItemsSource = null;
                }
            }
            else
            {
                MonitorDetailsPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateColorPreview(string hex)
        {
            try
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
                BtnColorPicker.Background = new SolidColorBrush(color);
            }
            catch
            {
                BtnColorPicker.Background = System.Windows.Media.Brushes.Black;
            }
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (Directory.Exists(TxtPlaylist.Text))
                {
                    dialog.SelectedPath = TxtPlaylist.Text;
                }

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    bool wasEmpty = string.IsNullOrWhiteSpace(TxtPlaylist.Text);
                    TxtPlaylist.Text = dialog.SelectedPath;
                    
                    // Auto-enable if it was empty
                    if (wasEmpty)
                    {
                        ChkEnabled.IsChecked = true;
                    }
                }
            }
        }

        private void BtnColorPicker_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new ColorDialog())
            {
                try {
                    var c = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(TxtColor.Text);
                    dialog.Color = System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);
                } catch {}

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var c = dialog.Color;
                    string hex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
                    TxtColor.Text = hex;
                    UpdateColorPreview(hex);
                }
            }
        }

        private void BtnSaveMonitor_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMonitor == null) return;

            try
            {
                _selectedMonitor.IsEnabled = ChkEnabled.IsChecked ?? true;
                _selectedMonitor.AudioEnabled = ChkAudio.IsChecked ?? true;
                _selectedMonitor.PlaylistFolder = TxtPlaylist.Text;
                
                // Convert hours, minutes, seconds to milliseconds
                if (int.TryParse(TxtHours.Text, out int hours) &&
                    int.TryParse(TxtMinutes.Text, out int minutes) &&
                    int.TryParse(TxtSeconds.Text, out int seconds))
                {
                    int totalMs = (hours * 3600 + minutes * 60 + seconds) * 1000;
                    // Allow 0 for "Never Rotate" (Manual only)
                    _selectedMonitor.RotationIntervalMs = Math.Max(0, totalMs); 
                }

                _selectedMonitor.BackgroundColor = TxtColor.Text;
                
                if (CmbStyle.SelectedItem is ComboBoxItem item)
                {
                    _selectedMonitor.WallpaperStyle = item.Content.ToString() ?? "Fill";
                }

                _selectedMonitor.ShuffleMode = (ShuffleMode)CmbShuffleMode.SelectedIndex;
                _selectedMonitor.RotateOnVideoEnd = ChkRotateOnVideoEnd.IsChecked ?? false;
                _selectedMonitor.BypassIntervalForLongVideo = ChkBypassInterval.IsChecked ?? true;

                // If in Clone or Stretch mode, apply to ALL monitors
                if (_config.GlobalSettings.DisplayMode != DisplayMode.PerDisplay)
                {
                    foreach (var m in _config.Monitors)
                    {
                        m.IsEnabled = _selectedMonitor.IsEnabled;
                        m.AudioEnabled = _selectedMonitor.AudioEnabled;
                        m.PlaylistFolder = _selectedMonitor.PlaylistFolder;
                        m.RotationIntervalMs = _selectedMonitor.RotationIntervalMs;
                        m.BackgroundColor = _selectedMonitor.BackgroundColor;
                        m.WallpaperStyle = _selectedMonitor.WallpaperStyle;
                        m.ShuffleMode = _selectedMonitor.ShuffleMode;
                        m.RotateOnVideoEnd = _selectedMonitor.RotateOnVideoEnd;
                        m.BypassIntervalForLongVideo = _selectedMonitor.BypassIntervalForLongVideo;
                        // Also sync CurrentWallpaper if possible to keep them in sync?
                        // RotationEngine handles sync in Clone/Stretch mode, so we just need config sync.
                    }
                }

                ConfigManager.SaveConfig(_config);
                _service.RefreshPlaylists();
                _service.ReapplySettings(true); // Preserve current wallpaper
                
                System.Windows.MessageBox.Show(this, "Monitor settings saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(this, $"Error saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRotateNow_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMonitor != null)
            {
                // Force enable if disabled
                if (!_selectedMonitor.IsEnabled)
                {
                    _selectedMonitor.IsEnabled = true;
                    ChkEnabled.IsChecked = true;
                    // We need to save this state change or at least apply it
                    // ManualRotate in service calls RotateMonitor which checks IsEnabled.
                    // But we also need to ensure the window exists.
                    // Let's just update the UI and object, the service call might need to force it.
                    // Actually, ManualRotate calls RotateMonitor. RotateMonitor checks IsEnabled.
                    // So setting it here affects the object.
                    // But we should probably save it too? Or just let the user save later?
                    // Better to just let it run.
                }

                _service.ManualRotate(_selectedMonitor.MonitorId);
                // Refresh list after rotation (might need a delay or event, but for now user can re-select)
                // Actually, we can just refresh the list manually
                if (_selectedMonitor.WallpaperHistory != null)
                {
                     ListHistory.ItemsSource = _selectedMonitor.WallpaperHistory.AsEnumerable().Reverse().ToList();
                }
            }
        }

        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMonitor != null && Directory.Exists(_selectedMonitor.PlaylistFolder))
            {
                Process.Start("explorer.exe", _selectedMonitor.PlaylistFolder);
            }
            else
            {
                System.Windows.MessageBox.Show(this, "Folder does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnSetCurrent_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMonitor == null || string.IsNullOrEmpty(_selectedMonitor.PlaylistFolder) || !Directory.Exists(_selectedMonitor.PlaylistFolder))
            {
                System.Windows.MessageBox.Show(this, "Please select a valid playlist folder first.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var dialog = new OpenFileDialog())
            {
                dialog.InitialDirectory = _selectedMonitor.PlaylistFolder;
                dialog.Filter = "Media Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.mp4;*.webm;*.avi;*.mkv;*.m4v|All Files|*.*";
                dialog.Title = "Select Starting Wallpaper";
                
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    // Force enable
                    if (!_selectedMonitor.IsEnabled)
                    {
                        _selectedMonitor.IsEnabled = true;
                        ChkEnabled.IsChecked = true;
                    }

                    // Save the selection
                    _selectedMonitor.CurrentWallpaper = dialog.FileName;
                    
                    // Save config to ensure persistence
                    ConfigManager.SaveConfig(_config);
                    
                    // Force display immediately
                    _service.SetSpecificWallpaper(_selectedMonitor.MonitorId, dialog.FileName);
                    
                    // Refresh history list
                    if (_selectedMonitor.WallpaperHistory != null)
                    {
                        ListHistory.ItemsSource = _selectedMonitor.WallpaperHistory.AsEnumerable().Reverse().ToList();
                    }

                    System.Windows.MessageBox.Show(this, "Wallpaper set and displayed.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void BtnLocateCurrent_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMonitor != null && !string.IsNullOrEmpty(_selectedMonitor.CurrentWallpaper) && File.Exists(_selectedMonitor.CurrentWallpaper))
            {
                string argument = "/select, \"" + _selectedMonitor.CurrentWallpaper + "\"";
                Process.Start("explorer.exe", argument);
            }
            else
            {
                System.Windows.MessageBox.Show(this, "Current wallpaper file not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ListHistory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListHistory.SelectedItem is string path)
            {
                if (File.Exists(path))
                {
                    string argument = "/select, \"" + path + "\"";
                    Process.Start("explorer.exe", argument);
                }
                else
                {
                    System.Windows.MessageBox.Show(this, "File not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                
                // Deselect to allow clicking again
                ListHistory.SelectedItem = null;
            }
        }

        // --- Global Logic ---
        private void BtnSaveGlobal_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _config.GlobalSettings.GlobalAudioEnabled = ChkGlobalAudio.IsChecked ?? true;
                _config.GlobalSettings.MinimizeToTray = ChkMinimizeTray.IsChecked ?? true;
                _config.GlobalSettings.DebugLogging = ChkDebugLog.IsChecked ?? false;
                _config.GlobalSettings.EnableDoubleClickRotate = ChkDoubleClickRotate.IsChecked ?? false;
                _config.GlobalSettings.EnableHoverActions = ChkHoverActions.IsChecked ?? false;
                _config.GlobalSettings.DeleteWithoutConfirmation = ChkDeleteNoConfirm.IsChecked ?? false;
                _config.GlobalSettings.DisplayMode = (DisplayMode)CmbDisplayMode.SelectedIndex;

                bool startWithWindows = ChkStartWindows.IsChecked ?? false;
                _service.SetStartWithWindows(startWithWindows);

                ConfigManager.SaveConfig(_config);
                
                // Refresh monitor list in case mode changed
                RefreshMonitorList();
                
                _service.ReapplySettings(true); // Preserve current wallpaper

                System.Windows.MessageBox.Show(this, "Global settings saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(this, $"Error saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- Logs Logic ---
        private void BtnRefreshLogs_Click(object sender, RoutedEventArgs e)
        {
            RefreshLogs();
        }

        private void BtnOpenLogFile_Click(object sender, RoutedEventArgs e)
        {
            string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log");
            if (File.Exists(logPath))
            {
                Process.Start("notepad.exe", logPath);
            }
        }

        private void RefreshLogs()
        {
            try
            {
                string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log");
                
                if (File.Exists(logPath))
                {
                    using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs))
                    {
                        if (fs.Length > 20000)
                        {
                            fs.Seek(-20000, SeekOrigin.End);
                        }
                        TxtLogs.Text = sr.ReadToEnd();
                        TxtLogs.ScrollToEnd();
                    }
                }
                else
                {
                    TxtLogs.Text = "Log file not found.";
                }
            }
            catch (Exception ex)
            {
                TxtLogs.Text = $"Failed to read logs: {ex.Message}";
            }
        }

        // --- Time Picker Button Handlers ---
        private void BtnHoursUp_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtHours.Text, out int value))
            {
                TxtHours.Text = (value + 1).ToString();
            }
        }

        private void BtnHoursDown_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtHours.Text, out int value))
            {
                TxtHours.Text = Math.Max(0, value - 1).ToString();
            }
        }

        private void BtnMinutesUp_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtMinutes.Text, out int value))
            {
                TxtMinutes.Text = ((value + 1) % 60).ToString();
            }
        }

        private void BtnMinutesDown_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtMinutes.Text, out int value))
            {
                TxtMinutes.Text = Math.Max(0, value - 1).ToString();
            }
        }

        private void BtnSecondsUp_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtSeconds.Text, out int value))
            {
                TxtSeconds.Text = ((value + 5) % 60).ToString();
            }
        }

        private void BtnSecondsDown_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtSeconds.Text, out int value))
            {
                TxtSeconds.Text = Math.Max(0, value - 5).ToString();
            }
        }
    }
}
