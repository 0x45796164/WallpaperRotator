using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Imaging;
using WallpaperRotator.Core;
using WallpaperRotator.Utilities;

namespace WallpaperRotator.UI
{
    public partial class WallpaperView : System.Windows.Controls.UserControl
    {
        public event EventHandler? VideoEnded;
        public event EventHandler? RequestPause;
        public event EventHandler? RequestResume;
        public event EventHandler? RequestRotate;
        public event EventHandler? RequestPrevious;
        
        private string? _currentPath;
        private bool _isAudioEnabled = true;

        public WallpaperView()
        {
            InitializeComponent();
            this.MouseDoubleClick += OnMouseDoubleClick;
        }

        private void OnMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ConfigManager.CurrentConfig.GlobalSettings.EnableDoubleClickRotate)
            {
                RequestRotate?.Invoke(this, EventArgs.Empty);
            }
        }

        private void HoverOverlay_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (ConfigManager.CurrentConfig.GlobalSettings.EnableHoverActions)
            {
                HoverOverlay.Opacity = 1;
                RequestPause?.Invoke(this, EventArgs.Empty);
            }
        }

        private void HoverOverlay_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            HoverOverlay.Opacity = 0;
            RequestResume?.Invoke(this, EventArgs.Empty);
        }

        private void BtnPrevious_Click(object sender, RoutedEventArgs e)
        {
            RequestPrevious?.Invoke(this, EventArgs.Empty);
        }

        private void BtnOpenExplorer_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentPath) && System.IO.File.Exists(_currentPath))
            {
                System.Diagnostics.Process.Start("explorer.exe", "/select, \"" + _currentPath + "\"");
            }
        }

        private void BtnOpenApp_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentPath) && System.IO.File.Exists(_currentPath))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_currentPath) { UseShellExecute = true });
                }
                catch { }
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentPath) && System.IO.File.Exists(_currentPath))
            {
                bool confirm = !ConfigManager.CurrentConfig.GlobalSettings.DeleteWithoutConfirmation;
                
                if (confirm)
                {
                    var result = System.Windows.MessageBox.Show($"Are you sure you want to delete this file?\n{_currentPath}", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result != MessageBoxResult.Yes) return;
                }

                try
                {
                    if (FileOperations.DeleteToRecycleBin(_currentPath))
                    {
                        RequestRotate?.Invoke(this, EventArgs.Empty); // Rotate away
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Failed to move file to Recycle Bin.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to delete: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void SetAudio(bool enabled)
        {
            _isAudioEnabled = enabled;
            if (Vid != null)
            {
                Vid.Volume = _isAudioEnabled ? 1.0 : 0.0;
            }
        }

        public void SetBackground(string hexColor)
        {
            try
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hexColor);
                this.Background = new SolidColorBrush(color);
            }
            catch
            {
                this.Background = System.Windows.Media.Brushes.Black;
            }
        }

        public void SetImage(string path, Stretch stretch)
        {
            _currentPath = path;
            Vid.Stop();
            Vid.Visibility = Visibility.Collapsed;
            Vid.Source = null;

            try
            {
                Logger.Info($"[WPF] Loading image: {path}");
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path);
                bitmap.EndInit();
                bitmap.Freeze();

                Img.Source = bitmap;
                Img.Stretch = stretch;
                Img.Visibility = Visibility.Visible;
                Logger.Info($"[WPF] Image loaded successfully.");
            }
            catch (Exception ex)
            {
                Logger.Error($"[WPF] Failed to load image: {ex.Message}");
            }
        }

        public void SetVideo(string path, Stretch stretch)
        {
            _currentPath = path;
            Img.Visibility = Visibility.Collapsed;
            Img.Source = null;

            try
            {
                Logger.Info($"[WPF] Loading video: {path}");
                Vid.Source = new Uri(path);
                Vid.Stretch = stretch;
                Vid.Visibility = Visibility.Visible;
                Vid.Volume = _isAudioEnabled ? 1.0 : 0.0;
                
                // Clear previous handlers
                Vid.MediaEnded -= OnMediaEnded;
                Vid.MediaEnded += OnMediaEnded;
                
                Vid.Play();
                Logger.Info($"[WPF] Video started.");
            }
            catch (Exception ex)
            {
                Logger.Error($"[WPF] Failed to load video: {ex.Message}");
            }
        }

        private void OnMediaEnded(object sender, RoutedEventArgs e)
        {
            // Notify engine
            VideoEnded?.Invoke(this, EventArgs.Empty);
            
            // Loop by default
            Vid.Position = TimeSpan.Zero;
            Vid.Play();
        }
    }
}
