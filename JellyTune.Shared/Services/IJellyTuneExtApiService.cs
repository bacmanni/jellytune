using JellyTune.Shared.Models;

namespace JellyTune.Shared.Services;

public interface IJellyTuneExtApiService
{
    public Task<Artist?> GetArtistAsync(string artistName);
    public Task<byte[]?> GetArtistImageAsync(string url);
}