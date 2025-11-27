using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using WallpaperRotator.Utilities;

namespace WallpaperRotator.UI;

public class WallpaperWindow : Window
{
    private WallpaperView _view;
    private string _monitorId;
    public event EventHandler? VideoEnded;

    public WallpaperWindow(string monitorId, System.Drawing.Rectangle bounds)
    {
        _monitorId = monitorId;
        
        // Window Setup
        this.WindowStyle = WindowStyle.None;
        this.ResizeMode = ResizeMode.NoResize;
        this.ShowInTaskbar = false;
        this.Background = System.Windows.Media.Brushes.Black;
        this.Left = bounds.Left;
        this.Top = bounds.Top;
        this.Width = bounds.Width;
        this.Height = bounds.Height;
        
        // Ensure it loads
        this.Loaded += OnLoaded;

        InitializeComponent();
        
        // Hook view event
        if (_view != null)
        {
            _view.VideoEnded += (s, e) => VideoEnded?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Any post-load setup if needed
    }

    private void InitializeComponent()
    {
        _view = new WallpaperView();
        this.Content = _view;
    }

    public void UpdateSettings(bool audioEnabled, string hexColor)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => UpdateSettings(audioEnabled, hexColor));
            return;
        }
        _view.SetAudio(audioEnabled);
        _view.SetBackground(hexColor);
    }

    public void SetContent(string path, string style, bool audioEnabled, string hexColor)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => SetContent(path, style, audioEnabled, hexColor));
            return;
        }

        try
        {
            string styleLog = style ?? "null(default:fill)";
            string colorLog = hexColor ?? "null(default:#000000)";
            Logger.Info($"Setting content on {_monitorId}: {path} (Style: {styleLog}, Audio: {audioEnabled}, Color: {colorLog})");
            
            // Update settings first
            _view.SetAudio(audioEnabled);
            _view.SetBackground(hexColor ?? "#000000");

            if (System.IO.File.Exists(path))
            {
                // Map style string to WPF Stretch
                Stretch wpfStretch = Stretch.UniformToFill; // Default Fill
                switch ((style ?? "fill").ToLower())
                {
                    case "fill": wpfStretch = Stretch.UniformToFill; break;
                    case "fit": wpfStretch = Stretch.Uniform; break;
                    case "stretch": wpfStretch = Stretch.Fill; break;
                    case "center": wpfStretch = Stretch.None; break;
                    case "cover": wpfStretch = Stretch.UniformToFill; break; 
                }

                // Check extension for video
                string ext = System.IO.Path.GetExtension(path).ToLower();
                if (ext == ".mp4" || ext == ".webm" || ext == ".avi" || ext == ".mkv" || ext == ".m4v" || ext == ".gif")
                {
                    _view.SetVideo(path, wpfStretch);
                }
                else
                {
                    _view.SetImage(path, wpfStretch);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to set content on {_monitorId}: {ex.Message}");
        }
    }

    // Helper to get Handle for P/Invoke
    public IntPtr GetHandle()
    {
        return new WindowInteropHelper(this).Handle;
    }
}
