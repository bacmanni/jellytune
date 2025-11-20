using JellyTune.Shared.Enums;

namespace JellyTune.Shared.Services;

public interface IFileService
{
    public Task<byte[]?> GetFileAsync(FileType type, Guid id);
}