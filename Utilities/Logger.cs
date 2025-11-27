using System;
using System.IO;
using System.Diagnostics;

namespace WallpaperRotator.Utilities;

public static class Logger
{
    private static readonly string LOG_FOLDER = AppDomain.CurrentDomain.BaseDirectory;
    
    private static readonly string LOG_FILE_PATH = Path.Combine(LOG_FOLDER, "debug.log");
    private static readonly object _lock = new();

    static Logger()
    {
        try
        {
            if (!Directory.Exists(LOG_FOLDER))
            {
                Directory.CreateDirectory(LOG_FOLDER);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to create log directory: {ex.Message}");
        }
    }

    public static void Info(string message) => Log("INFO", message);
    public static void Warn(string message) => Log("WARN", message);
    public static void Error(string message) => Log("ERROR", message);
    public static void DebugLog(string message) => Log("DEBUG", message); // Renamed to avoid conflict with Debug.WriteLine

    private static void Log(string level, string message)
    {
        string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
        
        // Console output
        Console.WriteLine(logEntry);
        
        // Debug output
        Debug.WriteLine(logEntry);
        
        // File output
        try
        {
            lock (_lock)
            {
                File.AppendAllText(LOG_FILE_PATH, logEntry + Environment.NewLine);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to write log: {ex.Message}");
        }
    }
    
    public static void RotateLogFile()
    {
        try
        {
            lock (_lock)
            {
                var info = new FileInfo(LOG_FILE_PATH);
                if (info.Exists && info.Length > 10 * 1024 * 1024) // 10MB
                {
                    string archivePath = LOG_FILE_PATH + $".{DateTime.Now:yyyyMMdd_HHmmss}";
                    File.Move(LOG_FILE_PATH, archivePath);
                }
            }
        }
        catch (Exception ex)
        {
            Error($"Failed to rotate log: {ex.Message}");
        }
    }
}
