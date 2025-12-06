using JellyTune.Shared.Enums;

namespace JellyTune.Shared.Services;

public interface IFileService
{
    public Task<byte[]?> GetFileAsync(FileType type, Guid id, CancellationToken cancellationToken = default);
    public Uri? GetFileUrl(FileType type, Guid id);
}