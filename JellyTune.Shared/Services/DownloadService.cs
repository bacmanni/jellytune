using System.Collections.Concurrent;
using System.IO.Abstractions;
using JellyTune.Shared.Events;
using JellyTune.Shared.Models;

namespace JellyTune.Shared.Services;

public class DownloadService : IDownloadService
{
    private readonly IJellyTuneApiService _jellyTuneApiService;
    private readonly IFileSystem _fileSystem;
    private readonly IConfigurationService _configurationService;

    private readonly SemaphoreSlim _downloadSemaphore = new(1, 1);
    private readonly ConcurrentDictionary<Guid, List<Track>> _downloads = new();
    private readonly CancellationTokenSource _downloadCts = new();
    private Guid? _downloadingParentId;
    
    public event EventHandler<DownloadArgs>? OnDownloadProgressChanged;
    
    public DownloadService(IJellyTuneApiService jellyTuneApiService, IFileSystem fileSystem, IConfigurationService configurationService)
    {
        _jellyTuneApiService = jellyTuneApiService;
        _fileSystem = fileSystem;
        _configurationService = configurationService;
    }

    private string GetRootPath()
    {
        return $"{_configurationService.GetCacheDirectory()}/jellytune/artists";
    }
    
    private string GetParentPath(Guid parent)
    {
        return $"{GetRootPath()}/{parent}/";
    }
    
    private string GetTrackFileName(Guid parent, Track track)
    {
        return $"{GetRootPath()}/{parent}/{track.Id}.{track.Container}";
    }

    /// <summary>
    /// Check if we are downloading currently
    /// </summary>
    /// <returns></returns>
    public bool IsDownloading(Guid? parent)
    {
        if (parent.HasValue)
            return _downloads.ContainsKey(parent.Value);
            
        return !_downloads.IsEmpty;
    }

    /// <summary>
    /// Check if parent has something stored in cache. If is, we consider all tracks to be downloaded
    /// </summary>
    /// <param name="parent"></param>
    /// <returns></returns>
    public bool HasParent(Guid parent)
    {
        var directory = GetParentPath(parent);
        if (!_fileSystem.Directory.Exists(directory)) return false;
        
        return _fileSystem.Directory.EnumerateFiles(directory).Any();
    }

    public long GetDownloadedTrackSize()
    {
        if (!_fileSystem.Directory.Exists(GetRootPath()))
            return 0;
            
        var size = _fileSystem.Directory
            .EnumerateFiles(GetRootPath(), "*", SearchOption.AllDirectories)
            .Sum(file => _fileSystem.FileInfo.New(file).Length);
        return size;
    }

    /// <summary>
    /// Check if track is found in cache
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="track"></param>
    /// <returns></returns>
    public bool HasTrackFile(Guid parent, Track track)
    {
        var filename = GetTrackFileName(parent, track);
        return _fileSystem.File.Exists(filename);
    }

    /// <summary>
    /// Get file location for streaming
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="track"></param>
    /// <returns></returns>
    public Stream GetAudioStream(Guid parent, Track track)
    {
        var filename = GetTrackFileName(parent, track);
        
        var bytes = _fileSystem.File.ReadAllBytes(filename);
        return new MemoryStream(bytes);
    }

    /// <summary>
    /// Cancel active download if there is any
    /// </summary>
    public void CancelDownload()
    {
        Console.WriteLine("Cancellation requested");
        
        // Cancel downloads
        _downloadCts?.Cancel();

        if (_downloadingParentId.HasValue)
        {
            Console.WriteLine($"Deleted parent: {_downloadingParentId}");
            DeleteParent(_downloadingParentId.Value);
        }
    }

    /// <summary>
    /// Delete all downloaded tracks from device
    /// </summary>
    public void DeleteAll()
    {
        var dir = _fileSystem.Path.GetDirectoryName(GetRootPath());
        if (_fileSystem.Directory.Exists(dir))
            _fileSystem.Directory.Delete(dir, true);
    }

    /// <summary>
    /// Store specific track audio to device cache
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="track"></param>
    /// <param name="cancellationToken"></param>
    public async Task WriteTrackFileAsync(Guid parent, Track track, CancellationToken cancellationToken = default)
    {
        var filename = GetTrackFileName(parent, track);
        
        var dir = _fileSystem.Path.GetDirectoryName(filename);
        if (dir is not null && !_fileSystem.Directory.Exists(dir))
            _fileSystem.Directory.CreateDirectory(dir);
        
        if (!_fileSystem.File.Exists(filename))
            _fileSystem.File.Create(filename).Close();
        
        var trackBytes = await _jellyTuneApiService.DownloadTrackAsync(track.Id, cancellationToken);
        await _fileSystem.File.WriteAllBytesAsync(filename, trackBytes, cancellationToken);  
    }

    /// <summary>
    /// Read stored track audio. Null if not found
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="track"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<byte[]?> ReadTrackFileAsync(Guid parent, Track track, CancellationToken cancellationToken = default)
    {
        var filename = GetTrackFileName(parent, track);
        if (!_fileSystem.File.Exists(filename)) return null;
        
        return await _fileSystem.File.ReadAllBytesAsync(filename, cancellationToken);  
    }

    /// <summary>
    /// Delete everything under parent
    /// </summary>
    /// <param name="parent"></param>
    public void DeleteParent(Guid parent)
    {
        var directory = GetParentPath(parent);
        if (!_fileSystem.Directory.Exists(directory)) return;
        
        _fileSystem.Directory.Delete(directory, true);
        
        OnDownloadProgressChanged?.Invoke(this, new DownloadArgs());
    }

    /// <summary>
    /// Download set of tracks to file system
    /// </summary>
    /// <param name="parentId"></param>
    /// <param name="tracks"></param>
    public async Task DownloadTracksAsync(Guid parentId, List<Track> tracks)
    {
        if (!_downloads.TryAdd(parentId, tracks.ToList()))
            throw new InvalidOperationException(
                $"Download already queued for {parentId}");
        
        var token = _downloadCts.Token;
        try
        {
            await _downloadSemaphore.WaitAsync(token);
            Console.WriteLine("Download started");
            
            while (!_downloads.IsEmpty)
            {
                token.ThrowIfCancellationRequested();

                var item = _downloads.FirstOrDefault();
                if (item.Value is null)
                    break;

                _downloadingParentId = item.Key;
                var progress = 1;
                foreach (var track in item.Value)
                {
                    token.ThrowIfCancellationRequested();
                    
                    var totalCount = _downloads.Sum(d => d.Value.Count);
                    OnDownloadProgressChanged?.Invoke(this,
                        new DownloadArgs()
                        {
                            ParentId = item.Key, TrackNumber = progress, TotalTrackCount = totalCount,
                            TrackName = track.Name
                        });
                    
                    Console.WriteLine($"Downloading: {track.Name}");
                    await WriteTrackFileAsync(item.Key, track, token);
                    progress++;
                }

                _downloads.TryRemove(item);
            }
        }
        finally
        {
            _downloadingParentId = null;
            _downloadSemaphore.Release();
        }
        
        OnDownloadProgressChanged?.Invoke(this, new DownloadArgs());
    }
}