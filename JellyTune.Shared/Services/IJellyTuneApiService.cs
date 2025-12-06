using JellyTune.Shared.Enums;
using JellyTune.Shared.Models;

namespace JellyTune.Shared.Services;

public interface IJellyTuneApiService
{
    public bool SetServer(string serverUrl);
    public void SetCollectionId(Guid collectionId);
    public Guid? GetCollectionId();
    public Task<bool> CheckServerAsync(string serverUrl);
    public Task<bool> LoginAsync(string username, string password);
    public Task<string> StartPlaybackAsync(Guid trackId);
    public Task PausePlaybackAsync(string sessiondId, Guid trackId, int? position);
    public Task<string?> GetTrackLyricsAsync(Guid trackId);
    public Task<List<Collection>> GetCollectionsAsync(CollectionType type);
    public Task<List<Models.Album>> GetArtistsAndAlbumsAsync(int? startIndex = null,
        int? count = null);
    public Task<Album> GetAlbumAsync(Guid albumId, CancellationToken cancellationToken = default);
    public Task<List<Search>> SearchAlbum(string value, CancellationToken cancellationToken = default);
    public Task<List<Models.Search>> SearchArtistAlbums(string value, CancellationToken cancellationToken = default);
    public Task<List<Models.Search>> SearchTrack(string value, CancellationToken cancellationToken = default);
    public Task<List<Track>> GetTracksAsync(Guid albumId, CancellationToken cancellationToken = default);
    public Task<Models.Track> GetTrackAsync(Guid trackId);
    public Task<byte[]?> GetPrimaryArtAsync(Guid albumId);
    public Task<Stream?> GetAudioStreamAsync(Guid trackId);
    public Task<Playlist> GetPlaylistAsync(Guid playlistId);
    public Task<List<Track>> GetPlaylistTracksAsync(Guid playlistId);
    public string GetAudioStreamUrl(string sessiondId, Guid trackId, int? position = null);
    public Task StopPlaybackAsync(string sessiondId, Guid trackId);
    public Task ResumePlaybackAsync(string sessiondId, Guid trackId, int? position);
    public Task<List<Playlist>> GetPlaylistsAsync(Guid collectionId);
    Uri? GetPrimaryArtUrl(Guid id);
    public string GetWebsocketUrl();
    public Task SeekPlaybackAsync(string sessiondId, Guid trackId, int? position);
}