using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;

namespace JellyTune.Shared.Controls;

public sealed class PlaylistTracksController : IDisposable
{
    private readonly IJellyTuneApiService _jellyTuneApiService;
    private readonly IPlayerService _playerService;
    private readonly IFileService _fileService;
    
    public IFileService FileService => _fileService;
    public IPlayerService PlayerService => _playerService;

    private CancellationTokenSource? _openPlaylistCts;
    
    public Playlist? Playlist { private set; get; }
    public readonly List<Track> Tracks = [];
    public event EventHandler<PlaylistTracksStateArgs>? OnPlaylistTracksStateChanged;
    
    public PlaylistTracksController(IJellyTuneApiService jellyTuneApiService, IPlayerService playerService, IFileService fileService)
    {
        _jellyTuneApiService = jellyTuneApiService;
        _playerService = playerService;
        _fileService = fileService;
        
        _playerService.OnPlayerStateChanged += PlayerServiceOnPlayerStateChanged;
    }

    private void PlayerServiceOnPlayerStateChanged(object? sender, PlayerStateArgs e)
    {
        if (e.State is PlayerState.Playing or PlayerState.Stopped or PlayerState.Paused or PlayerState.Starting)
        {
            OnPlaylistTracksStateChanged?.Invoke(this, new PlaylistTracksStateArgs {  UpdateTrackState = true, SelectedTrackId = e.SelectedTrack != null ? e.SelectedTrack.Id : e.SelectedTrackId });
        }
    }
    
    /// <summary>
    /// Open selected playlist tracks
    /// </summary>
    /// <param name="playlistId"></param>
    public async Task OpenPlaylist(Guid playlistId)
    {
        _openPlaylistCts?.Cancel();
        _openPlaylistCts?.Dispose();
        
        _openPlaylistCts = new CancellationTokenSource();
        var cancellationToken = _openPlaylistCts.Token;
        
        OnPlaylistTracksStateChanged?.Invoke(this, new PlaylistTracksStateArgs { Loading = true });
        
        try
        {
            Playlist = await _jellyTuneApiService.GetPlaylistAsync(playlistId);
            cancellationToken.ThrowIfCancellationRequested();
            
            var tracks = await _jellyTuneApiService.GetPlaylistTracksAsync(playlistId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            
            Tracks.Clear();
            Tracks.AddRange(tracks);
            OnPlaylistTracksStateChanged?.Invoke(this, new PlaylistTracksStateArgs());
        }
        catch (OperationCanceledException)
        {
            // A newer OpenPlaylist call cancelled this one.
        }
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