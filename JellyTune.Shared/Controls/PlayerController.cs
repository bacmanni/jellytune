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
    
    public IPlayerService PlayerService => _playerService;
    public IJellyTuneApiService JellyTuneApiService => _jellyTuneApiService;
    
    public IConfigurationService ConfigurationService => _configurationService;
    
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

    /// <summary>
    /// Seek track 
    /// </summary>
    /// <param name="value">Seconds</param>
    public void SeekTrack(double value)
    {
        _playerService.SeekTrack(value);
    }
}