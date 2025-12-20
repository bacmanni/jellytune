using JellyTune.Shared.Enums;

namespace JellyTune.Shared.Services;

public interface IFileService
{
    public Task<byte[]?> GetFileAsync(FileType type, Guid id, CancellationToken cancellationToken = default);
    public Task<T?> GetCacheFile<T>(string fileName, CancellationToken cancellationToken = default);
    public Task WriteCacheFile<T>(string id, T data);
    public void ClearCacheFile(string id);
    public Uri? GetFileUrl(FileType type, Guid id);
}