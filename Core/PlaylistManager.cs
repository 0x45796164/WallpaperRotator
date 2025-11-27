using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WallpaperRotator.Core.Models;
using WallpaperRotator.Utilities;

namespace WallpaperRotator.Core;

public class PlaylistManager : IDisposable
{
    private readonly ConcurrentDictionary<string, Playlist> _playlists = new();
    private readonly ConcurrentDictionary<string, FileSystemWatcher> _watchers = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _debounceTokens = new();
    
    // Debounce interval for file system events
    private const int DEBOUNCE_DELAY_MS = 1000;

    public string? GetNext(string? folderPath, ShuffleMode mode, string? currentWallpaper)
    {
        if (string.IsNullOrEmpty(folderPath)) 
        {
            Logger.Warn("GetNext called with empty folder path.");
            return null;
        }

        if (!Directory.Exists(folderPath))
        {
            Logger.Warn($"Playlist folder not found: {folderPath}");
            return null;
        }

        var playlist = _playlists.GetOrAdd(folderPath, CreatePlaylist);
        
        if (playlist.Wallpapers.Count == 0)
        {
            Logger.Warn($"No wallpapers found in {folderPath}");
            return null;
        }

        return playlist.GetNext(mode, currentWallpaper);
    }

    public void RefreshPlaylist(string folderPath)
    {
        if (_playlists.TryGetValue(folderPath, out var playlist))
        {
            UpdatePlaylistFiles(playlist);
        }
    }

    private Playlist CreatePlaylist(string folderPath)
    {
        Logger.Info($"Creating playlist for: {folderPath}");
        var playlist = new Playlist { FolderPath = folderPath };
        UpdatePlaylistFiles(playlist);
        SetupWatcher(folderPath);
        return playlist;
    }

    private void UpdatePlaylistFiles(Playlist playlist)
    {
        try
        {
            var files = FileHelper.EnumerateFiles(playlist.FolderPath);
            
            // Only update if changed (simple count check or hash could be better, but count is fast)
            // For now, we just replace.
            
            // We need to be thread-safe here if RotationEngine is reading while we write.
            // Since we are replacing the List reference, it's atomic enough for a read-only consumer,
            // but Playlist.GetNext modifies state (indices).
            // Ideally Playlist should be thread-safe, but for now we'll rely on the fact that
            // RotationEngine is single-threaded (timer based) and this update happens on a background thread.
            // To be safe, we can lock the playlist object during update.
            
            lock (playlist)
            {
                playlist.Wallpapers = files;
                playlist.Shuffle();
            }
            
            Logger.Info($"Playlist updated for {playlist.FolderPath}. Count: {files.Count}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to update playlist {playlist.FolderPath}: {ex.Message}");
        }
    }

    private void SetupWatcher(string folderPath)
    {
        if (_watchers.ContainsKey(folderPath)) return;

        try
        {
            var watcher = new FileSystemWatcher(folderPath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
                Filter = "*.*"
            };

            watcher.Created += (s, e) => OnFileChanged(folderPath);
            watcher.Deleted += (s, e) => OnFileChanged(folderPath);
            watcher.Renamed += (s, e) => OnFileChanged(folderPath);
            
            watcher.EnableRaisingEvents = true;
            _watchers[folderPath] = watcher;
            Logger.Info($"Started watching {folderPath}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to setup watcher for {folderPath}: {ex.Message}");
        }
    }

    private void OnFileChanged(string folderPath)
    {
        // Debounce logic
        if (_debounceTokens.TryGetValue(folderPath, out var oldCts))
        {
            oldCts.Cancel();
            oldCts.Dispose();
        }

        var cts = new CancellationTokenSource();
        _debounceTokens[folderPath] = cts;

        Task.Delay(DEBOUNCE_DELAY_MS, cts.Token).ContinueWith(t =>
        {
            if (t.IsCanceled) return;

            Logger.DebugLog($"File changes detected in {folderPath}, refreshing playlist...");
            RefreshPlaylist(folderPath);
        });
    }

    public void Dispose()
    {
        foreach (var watcher in _watchers.Values)
        {
            watcher.Dispose();
        }
        _watchers.Clear();

        foreach (var cts in _debounceTokens.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }
        _debounceTokens.Clear();
    }
}
