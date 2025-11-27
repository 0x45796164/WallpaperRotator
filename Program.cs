using System;
using System.Windows.Forms;
using WallpaperRotator.Core;
using WallpaperRotator.Utilities;

namespace WallpaperRotator;

static class Program
{
    [STAThread]
    static void Main()
    {
        const string appName = "WallpaperRotator_SingleInstance_Mutex";
        bool createdNew;

        using (var mutex = new System.Threading.Mutex(true, appName, out createdNew))
        {
            if (!createdNew)
            {
                // App is already running
                MessageBox.Show("Wallpaper Rotator is already running.", "Wallpaper Rotator", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Initialize Logger
            Logger.Info("WallpaperRotator starting...");

            try
            {
                ApplicationConfiguration.Initialize();

                // Ensure WPF Application exists
                if (System.Windows.Application.Current == null)
                {
                    new System.Windows.Application();
                }

                var service = new WallpaperRotatorService();
                service.Start();

                // Keep app alive (system tray)
                Application.Run();
            }
            catch (Exception ex)
            {
                Logger.Error($"Fatal error: {ex}");
                MessageBox.Show($"Fatal error: {ex.Message}", "WallpaperRotator Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
