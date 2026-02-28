using JellyTune.Shared.Models.External;

namespace JellyTune.Shared.Services;

public interface IJellyTuneExtApiService
{
    public Task<Artist?> GetArtistAsync(string artistName);
    public Task<Album?> GetAlbumAsync(string artistName, string albumName);
}