using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Text.Json;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Models;

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

    /// <summary>
    /// Get filename for specific type
    /// </summary>
    /// <param name="type"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
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
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<byte[]?> GetFileAsync(FileType type, Guid id, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
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
                    if (cancellationToken.IsCancellationRequested)
                        return null;
                    
                    var fileBytes = await _fileSystem.File.ReadAllBytesAsync(filename);
                    _artWork.TryAdd(key, fileBytes);
                    return fileBytes;
                }
            }

            if (cancellationToken.IsCancellationRequested)
                return null;
            
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public async Task<T?> GetCacheFile<T>(string id, CancellationToken cancellationToken = default)
    {
        var filename = GetCacheFileName(id);
        if (_fileSystem.File.Exists(filename))
        {
            try
            {
                var json = await _fileSystem.File.ReadAllTextAsync(filename, cancellationToken);
                if (!string.IsNullOrEmpty(json))
                {
                    var data = JsonSerializer.Deserialize<T>(json);
                    return data;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return default;
            }
        }
        
        return default;
    }

    /// <summary>
    /// Write cache file
    /// </summary>
    /// <param name="id">Id for the data</param>
    /// <param name="data">Data that is parsed as json</param>
    /// <typeparam name="T"></typeparam>
    public async Task WriteCacheFile<T>(string id, T data)
    {
        var filename = GetCacheFileName(id);
        var json = JsonSerializer.Serialize(data);
        
        var dir = _fileSystem.Path.GetDirectoryName(filename);
        if (!_fileSystem.Directory.Exists(dir))
            _fileSystem.Directory.CreateDirectory(dir);
        
        if (!_fileSystem.File.Exists(filename))
            _fileSystem.File.CreateText(filename).Close();
        
        await _fileSystem.File.WriteAllTextAsync(filename, json);  
    }

    /// <summary>
    /// Removes cache file
    /// </summary>
    /// <param name="id"></param>
    public void ClearCacheFile(string id)
    {
        var filename = GetCacheFileName(id);
        
        if (_fileSystem.File.Exists(filename))
            _fileSystem.File.Delete(filename);
    }

    private string GetCacheFileName(string id)
    {
        return $"{_configurationService.GetCacheDirectory()}/cache/{id}.json";
    }
    
    /// <summary>
    /// Get file url
    /// </summary>
    /// <param name="type">File type</param>
    /// <param name="id">Item id</param>
    /// <returns></returns>
    public Uri? GetFileUrl(FileType type, Guid id)
    {
        var filename = GetFilename(type, id);
        
        if (_configurationService.Get().CacheAlbumArt)
            if (_fileSystem.File.Exists(filename))
                return new Uri(filename);
        
        return _jellyTuneApiService.GetPrimaryArtUrl(id);
    }
}
