using JellyTune.Shared.Events;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;

namespace JellyTune.Shared.Controls;

public sealed class PlayerController : IDisposable
{
    private readonly IJellyTuneApiService _jellyTuneApiService;
    private readonly IConfigurationService _configurationService;
    private readonly IPlayerService _playerService;

    public event EventHandler<AlbumArgs> OnShowPlaylistClicked;
    
    public event EventHandler<AlbumArgs> OnShowShowLyricsClicked;
    
    public IPlayerService GetPlayerService() => _playerService;
    public IJellyTuneApiService GetJellyTuneApiService() => _jellyTuneApiService;
    
    public Album? Album;
    public List<Track>? Tracks;
    public Track? SelectedTrack;
    public byte[]? Artwork { get; private set; }

    public PlayerController(IJellyTuneApiService jellyTuneApiService, IConfigurationService configurationService, IPlayerService playerService)
    {
        _jellyTuneApiService = jellyTuneApiService;
        _playerService = playerService;
        _configurationService = configurationService;
        _playerService.OnPlayerStateChanged += PlayerServiceOnPlayerStateChanged;
        _playerService.OnPlayerPositionChanged += PlayerServiceOnPlayerPositionChanged;
    }

    private void PlayerServiceOnPlayerPositionChanged(object? sender, PlayerPositionArgs e)
    {

    }

    private void PlayerServiceOnPlayerStateChanged(object? sender, PlayerStateArgs e)
    {
        Album = e.Album;
        Tracks = e.Tracks;
        SelectedTrack = e.SelectedTrack;
        Artwork = _playerService.GetArtwork();
    }

    public void Dispose()
    {
        _playerService.OnPlayerStateChanged -= PlayerServiceOnPlayerStateChanged;
    }
    
    /// <summary>
    /// Get lyrics for track
    /// </summary>
    /// <param name="trackId"></param>
    /// <returns></returns>
    public async Task<string?> GetLyrics(Guid trackId)
    {
         return await _jellyTuneApiService.GetTrackLyricsAsync(trackId);
    }

    /// <summary>
    /// Open currently playing/stopped album
    /// </summary>
    public void ShowPlaylist()
    {
        if (Album == null || SelectedTrack == null)
            return;
        
        OnShowPlaylistClicked?.Invoke(this, new AlbumArgs { AlbumId = Album.Id, TrackId = SelectedTrack.Id });
    }

    /// <summary>
    /// Show lyrics for playing/stopped album
    /// </summary>
    public void ShowShowLyrics()
    {
        if (Album == null || SelectedTrack == null)
            return;
        
        OnShowShowLyricsClicked?.Invoke(this, new AlbumArgs { AlbumId = Album.Id, TrackId = SelectedTrack.Id });
    }
}