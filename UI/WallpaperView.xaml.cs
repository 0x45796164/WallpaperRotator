using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WallpaperRotator.Utilities;

namespace WallpaperRotator.UI
{
    public partial class WallpaperView : System.Windows.Controls.UserControl
    {
        public event EventHandler? VideoEnded;
        private bool _isAudioEnabled = true;

        public WallpaperView()
        {
            InitializeComponent();
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
