using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;

namespace JellyTune.Shared.Controls;

public sealed class PlaylistTracksController : IDisposable
{
    private readonly IJellyTuneApiService _jellyTuneApiService;
    private readonly IConfigurationService _configurationService;
    private readonly IPlayerService _playerService;
    private readonly IFileService _fileService;
    
    public IFileService GetFileService() => _fileService;
    public IPlayerService GetPlayerService() => _playerService;

    public Playlist Playlist { private set; get; }
    public readonly List<Track> Tracks = [];
    public event EventHandler<PlaylistTracksStateArgs> OnPlaylistTracksStateChanged;
    
    public PlaylistTracksController(IJellyTuneApiService jellyTuneApiService, IConfigurationService configurationService, IPlayerService playerService, IFileService fileService)
    {
        _jellyTuneApiService = jellyTuneApiService;
        _configurationService = configurationService;
        _playerService = playerService;
        _fileService = fileService;
        
        _playerService.OnPlayerStateChanged += PlayerServiceOnPlayerStateChanged;
    }

    private void PlayerServiceOnPlayerStateChanged(object? sender, PlayerStateArgs e)
    {
        if (e.State is PlayerState.Playing or PlayerState.Stopped or PlayerState.Paused or PlayerState.Starting or PlayerState.Selected)
        {
            OnPlaylistTracksStateChanged.Invoke(this, new PlaylistTracksStateArgs() {  UpdateTrackState = true, SelectedTrackId = e.SelectedTrack?.Id });
        }
    }

    /// <summary>
    /// Open selected playlist tracks
    /// </summary>
    /// <param name="playlistId"></param>
    public async Task OpenPlaylist(Guid playlistId)
    {
        OnPlaylistTracksStateChanged.Invoke(this, new PlaylistTracksStateArgs() { Loading = true });
        Playlist = await _jellyTuneApiService.GetPlaylistAsync(playlistId);
        
        Tracks.Clear();
        var tracks = await _jellyTuneApiService.GetPlaylistTracksAsync(playlistId);
        Tracks.AddRange(tracks);
        OnPlaylistTracksStateChanged.Invoke(this, new PlaylistTracksStateArgs());
    }

    /// <summary>
    /// Start playing track from playlist. Adds playlist to queue if empty
    /// </summary>
    /// <param name="trackId"></param>
    public async Task PlayOrPauseTrackAsync(Guid trackId)
    {
        _playerService.ClearTracks();
        _playerService.AddTracks(Tracks);
        
        if (_playerService.IsPlaying() && _playerService.IsPlayingTrack(trackId))
        {
            _playerService.PauseTrack();
        }
        else
        {
            await _playerService.StartTrackAsync(trackId);
        }
    }

    public void Dispose()
    {
        _playerService.OnPlayerStateChanged -= PlayerServiceOnPlayerStateChanged;
    }
}