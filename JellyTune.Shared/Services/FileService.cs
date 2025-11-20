using System.Collections.Concurrent;
using System.IO.Abstractions;
using JellyTune.Shared.Enums;

namespace JellyTune.Shared.Services;

public class FileService : IFileService
{
    private readonly IFileSystem _fileSystem;
    private readonly IJellyTuneApiService _jellyTuneApiService;
    private readonly IConfigurationService  _configurationService;
    
    // Used for caching already fetched images
    private readonly ConcurrentDictionary<string, byte[]> _artWork = [];
    private readonly SemaphoreSlim _semaphore = new(3);
    
    public FileService(IJellyTuneApiService jellyTuneApiService, IConfigurationService configurationService, IFileSystem fileSystem)
    {
        _jellyTuneApiService = jellyTuneApiService;
        _configurationService = configurationService;
        _fileSystem = fileSystem;
    }

    private string GetFilename(FileType type, Guid id)
    {
        if (type == FileType.AlbumArt)
            return $"{_configurationService.GetCacheDirectory()}/albums/{id.ToString()}.jpg";
        if (type == FileType.Playlist)
            return $"{_configurationService.GetCacheDirectory()}/playlists/{id.ToString()}.jpg";
        
        throw new NotImplementedException($"File type {type} not implemented");
    }

    /// <summary>
    /// Get file. Uses disk cache if set in configuration
    /// </summary>
    /// <param name="type"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<byte[]?> GetFileAsync(FileType type, Guid id)
    {
        await _semaphore.WaitAsync();
        try
        {
            var key = $"{type.ToString()}-{id.ToString()}";
            var filename = GetFilename(type, id);
        
            if (type == FileType.AlbumArt && _artWork.TryGetValue(key, out var cachedArt))
            {
                return cachedArt;
            }

            if (_configurationService.Get().CacheAlbumArt)
            {
                var dir = _fileSystem.Path.GetDirectoryName(filename);
                if (!_fileSystem.Directory.Exists(dir))
                    _fileSystem.Directory.CreateDirectory(dir);

                if (_fileSystem.File.Exists(filename))
                {
                    var fileBytes = await _fileSystem.File.ReadAllBytesAsync(filename);
                    _artWork.TryAdd(key, fileBytes);
                    return fileBytes;
                }
            }

            var primaryArt = await _jellyTuneApiService.GetPrimaryArtAsync(id);
            if (primaryArt == null)
                return null;

            if (_configurationService.Get().CacheAlbumArt)
            {
                await _fileSystem.File.WriteAllBytesAsync(filename, primaryArt);
            }

            _artWork.TryAdd(key, primaryArt);
            return primaryArt;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
