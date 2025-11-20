using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;

namespace JellyTune.Shared.Controls;

public sealed class PlaylistController : IDisposable
{
    private readonly IJellyTuneApiService _jellyTuneApiService;
    private readonly IConfigurationService _configurationService;
    private readonly IPlayerService _playerService;
    private readonly IFileService _fileService;

    public IFileService GetFileService() => _fileService;
    
    public readonly List<Playlist> Playlists = [];
    public event EventHandler<PlaylistStateArgs> OnPlaylistClicked;
    public event EventHandler<PlaylistStateArgs> OnPlaylistStateChanged;
    
    public PlaylistController(IJellyTuneApiService jellyTuneApiService, IConfigurationService configurationService, IPlayerService playerService, IFileService fileService)
    {
        _jellyTuneApiService = jellyTuneApiService;
        _configurationService = configurationService;
        _playerService = playerService;
        _fileService = fileService;
    }

    /// <summary>
    /// Refresh playlist data
    /// </summary>
    /// <param name="reload"></param>
    public async Task RefreshPlaylist(bool reload = false)
    {
        OnPlaylistStateChanged.Invoke(this, new PlaylistStateArgs() { Loading = true });
        Playlists.Clear();

        if (Guid.TryParse(_configurationService.Get().PlaylistCollectionId, out var playlistCollectionId))
        {
            var playlists = await _jellyTuneApiService.GetPlaylistsAsync(playlistCollectionId);
            Playlists.AddRange(playlists);
            OnPlaylistStateChanged.Invoke(this, new PlaylistStateArgs() { PlaylistId = playlistCollectionId });
        }
        else
        {
            OnPlaylistStateChanged.Invoke(this, new PlaylistStateArgs());
        }
    }

    /// <summary>
    /// Open input playlist
    /// </summary>
    /// <param name="playlistId"></param>
    public void OpenPlaylist(Guid playlistId)
    {
        OnPlaylistClicked?.Invoke(this, new PlaylistStateArgs() { PlaylistId = playlistId });
    }
    
    public void Dispose()
    {

    }
}