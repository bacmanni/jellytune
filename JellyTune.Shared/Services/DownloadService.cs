using System.IO.Abstractions;
using JellyTune.Shared.Events;
using JellyTune.Shared.Models;

namespace JellyTune.Shared.Services;

public class DownloadService : IDownloadService
{
    private readonly IJellyTuneApiService _jellyTuneApiService;
    private readonly IFileSystem _fileSystem;
    private readonly IConfigurationService _configurationService;
    
    private Guid? _DownloadingParentId;
    
    public event EventHandler<DownloadArgs>? OnDownloadProgressChanged;
    
    public DownloadService(IJellyTuneApiService jellyTuneApiService, IFileSystem fileSystem, IConfigurationService configurationService)
    {
        _jellyTuneApiService = jellyTuneApiService;
        _fileSystem = fileSystem;
        _configurationService = configurationService;
    }

    private string GetParentPath(Guid parent)
    {
        return $"{_configurationService.GetCacheDirectory()}/jellytune/artists/{parent}/";
    }
    
    private string GetTrackFileName(Guid parent, Track track)
    {
        return $"{_configurationService.GetCacheDirectory()}/jellytune/artists/{parent}/{track.Id}.{track.Container}";
    }

    /// <summary>
    /// Check if we are downloading currently
    /// </summary>
    /// <returns></returns>
    public bool IsDownloading(Guid? parent)
    {
        if (parent.HasValue)
            return _DownloadingParentId == parent;
            
        return _DownloadingParentId.HasValue;
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
    /// Delete stored track audio if exists
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="track"></param>
    public void DeleteTrackFileAsync(Guid parent, Track track)
    {
        var filename = GetTrackFileName(parent, track);
        if (!_fileSystem.File.Exists(filename)) return;
        
        _fileSystem.File.Delete(filename);
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
        _DownloadingParentId = parentId;
        var progress = 1;
        foreach (var track in tracks)
        {
            OnDownloadProgressChanged?.Invoke(this, new DownloadArgs() { ParentId = parentId, TrackNumber = progress, TotalTrackCount = tracks.Count, TrackName =  track.Name });
            await WriteTrackFileAsync(parentId, track);
            progress++;
        }

        _DownloadingParentId = null;
        OnDownloadProgressChanged?.Invoke(this, new DownloadArgs());
    }
}