using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WallpaperRotator.Utilities;

public static class FileHelper
{
    private static readonly HashSet<string> _validExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".webp",
        ".mp4", ".webm", ".avi", ".mkv", ".m4v", ".gif"
    };

    public static List<string> EnumerateFiles(string folderPath)
    {
        var files = new List<string>();
        try
        {
            if (!Directory.Exists(folderPath))
            {
                return files;
            }

            // EnumerateFileSystemEntries is more efficient than GetFiles for large directories
            // We use a custom filter logic instead of passing search pattern to support multiple extensions
            foreach (var file in Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories))
            {
                if (IsValidWallpaper(file))
                {
                    files.Add(file);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error enumerating files in {folderPath}: {ex.Message}");
        }
        return files;
    }

    public static bool IsValidWallpaper(string filePath)
    {
        try
        {
            var ext = Path.GetExtension(filePath);
            return !string.IsNullOrEmpty(ext) && _validExtensions.Contains(ext);
        }
        catch
        {
            return false;
        }
    }

    public static bool TryGetFileSize(string filePath, out long size)
    {
        size = 0;
        try
        {
            var info = new FileInfo(filePath);
            if (info.Exists)
            {
                size = info.Length;
                return true;
            }
        }
        catch
        {
            // Ignore errors
        }
        return false;
    }
}
