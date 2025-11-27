using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WallpaperRotator.Utilities;

namespace WallpaperRotator.Core.Models;

public class Playlist
{
    public string FolderPath { get; set; } = string.Empty;
    public List<string> Wallpapers { get; set; } = new();
    
    private int[] _shuffledIndices = Array.Empty<int>();
    private int _currentIndex;

    public void Shuffle()
    {
        if (Wallpapers.Count == 0) return;

        _shuffledIndices = new int[Wallpapers.Count];
        for (int i = 0; i < Wallpapers.Count; i++)
        {
            _shuffledIndices[i] = i;
        }

        RandomShuffler.Shuffle(_shuffledIndices);
        _currentIndex = 0;
    }

    public string? GetNext(ShuffleMode mode, string? currentWallpaper)
    {
        if (Wallpapers.Count == 0) return null;

        if (mode == ShuffleMode.Random)
        {
            return GetNextRandom();
        }
        else
        {
            return GetNextSorted(mode, currentWallpaper);
        }
    }

    // Original parameterless GetNext for backward compatibility if needed, defaults to Random
    public string? GetNext()
    {
        return GetNextRandom();
    }

    private string? GetNextRandom()
    {
        if (Wallpapers.Count == 0) return null;

        if (_shuffledIndices.Length != Wallpapers.Count)
        {
            Shuffle();
        }

        if (_currentIndex >= _shuffledIndices.Length)
        {
            Shuffle();
        }

        int index = _shuffledIndices[_currentIndex++];
        if (index < Wallpapers.Count)
        {
            return Wallpapers[index];
        }
        
        // Fallback
        Shuffle();
        return Wallpapers.Count > 0 ? Wallpapers[0] : null;
    }

    private string? GetNextSorted(ShuffleMode mode, string? currentWallpaper)
    {
        if (Wallpapers.Count == 0) return null;

        List<string> sortedList;

        if (mode == ShuffleMode.SortedByName)
        {
            sortedList = Wallpapers.OrderBy(p => Path.GetFileName(p)).ToList();
        }
        else // SortedByDate
        {
            // Note: This hits the disk. For large playlists, we might want to cache this.
            // But since files can change, on-demand is safer for correctness.
            sortedList = Wallpapers.OrderByDescending(p => File.GetLastWriteTime(p)).ToList();
        }

        if (string.IsNullOrEmpty(currentWallpaper))
        {
            Logger.DebugLog($"GetNextSorted: Current is null, returning first: {Path.GetFileName(sortedList.FirstOrDefault())}");
            return sortedList.FirstOrDefault();
        }

        // Case-insensitive search
        int index = sortedList.FindIndex(p => p.Equals(currentWallpaper, StringComparison.OrdinalIgnoreCase));
        
        if (index == -1)
        {
            Logger.DebugLog($"GetNextSorted: Current '{Path.GetFileName(currentWallpaper)}' not found in list. Returning first.");
            // Current not found (maybe deleted or first run), start from beginning
            return sortedList.FirstOrDefault();
        }

        // Next index, wrapping around
        int nextIndex = (index + 1) % sortedList.Count;
        var next = sortedList[nextIndex];
        Logger.DebugLog($"GetNextSorted: Current '{Path.GetFileName(currentWallpaper)}' found at {index}. Next is '{Path.GetFileName(next)}' at {nextIndex}.");
        return next;
    }
}
