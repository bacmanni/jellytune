using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;

namespace JellyTune.Shared.Controls;

public sealed class AlbumController : IDisposable
{
    private readonly IJellyTuneApiService _jellyTuneApiService;
    private readonly IConfigurationService _configurationService;
    private readonly IPlayerService _playerService;
    private readonly IFileService _fileService;
    
    private CancellationTokenSource? _cancellationTokenSource;
    
    public IPlayerService GetPlayerService() => _playerService;
    public IFileService GetFileService() => _fileService;

    public Album? Album { get; private set; }
    public List<Track> Tracks { get; private set; } = [];
    public Track? SelectedTrack { get; private set; }
    public byte[]? Artwork { get; private set; }
    public event EventHandler<AlbumStateArgs> OnAlbumChanged;
    
    public AlbumController(IJellyTuneApiService jellyTuneApiService, IConfigurationService configurationService, IPlayerService playerService, IFileService fileService)
    {
        _jellyTuneApiService = jellyTuneApiService;
        _configurationService = configurationService;
        _playerService = playerService;
        _fileService = fileService;
        _playerService.OnPlayerStateChanged += PlayerServiceOnPlayerStateChanged;
    }

    private void PlayerServiceOnPlayerStateChanged(object? sender, PlayerStateArgs e)
    {
        if (e.State is PlayerState.Playing or PlayerState.Stopped or PlayerState.Paused or PlayerState.Selected or PlayerState.None)
        {
            AlbumChanged(new AlbumStateArgs() { UpdateTrackState = true, SelectedTrackId = e.SelectedTrack?.Id });
        }
    }

    /// <summary>
    /// Set track as selected
    /// </summary>
    /// <param name="trackId"></param>
    public void SelectTrack(Guid trackId)
    {
        SelectedTrack = Tracks.FirstOrDefault(t => t.Id == trackId);
    }

    public void Dispose()
    {
        _playerService.OnPlayerStateChanged -= PlayerServiceOnPlayerStateChanged;
    }

    private void AlbumChanged(AlbumStateArgs args)
    {
        OnAlbumChanged?.Invoke(this, args);
    }

    /// <summary>
    /// Open album
    /// </summary>
    /// <param name="albumId"></param>
    /// <param name="selectedTrackId"></param>
    public async Task Open(Guid albumId, Guid? selectedTrackId = null)
    {
        AlbumChanged(new AlbumStateArgs());
        
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
        
        Album = await _jellyTuneApiService.GetAlbumAsync(albumId, _cancellationTokenSource.Token);
        Tracks = await _jellyTuneApiService.GetTracksAsync(albumId,  _cancellationTokenSource.Token);
        
        AlbumChanged(new AlbumStateArgs() { UpdateAlbum = true, UpdateTracks = true, SelectedTrackId = selectedTrackId });

        if (_cancellationTokenSource.IsCancellationRequested)
            return;
        
        if (Album.HasArtwork)
        {
            Artwork = await _fileService.GetFileAsync(FileType.AlbumArt, albumId, _cancellationTokenSource.Token);
            AlbumChanged(new AlbumStateArgs() { UpdateArtwork = true});    
        }
    }

    /// <summary>
    /// Close album
    /// </summary>
    public async Task Close()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
    }
    
    /// <summary>
    /// Play track. If already playing, then pause track
    /// </summary>
    /// <param name="trackId"></param>
    public async Task PlayOrPauseTrack(Guid trackId)
    {
        if (_playerService.IsPlaying() && _playerService.IsPlayingTrack(trackId))
        {
            _playerService.PauseTrack();
        }
        else
        {
            await _playerService.StartTrackAsync(trackId);
        }
    }

    /// <summary>
    /// Add track to play queue
    /// </summary>
    /// <param name="getTrackId"></param>
    public void AddTrackToQueue(Guid getTrackId)
    {
        var track =  Tracks.FirstOrDefault(t => t.Id == getTrackId);
        if (track != null)
            _playerService.AddTrack(track);
    }

    /// <summary>
    /// Get track position in queue
    /// </summary>
    /// <param name="trackId"></param>
    /// <returns></returns>
    public int? GetTrackPositionInQueue(Guid trackId)
    {
        var track = Tracks.FirstOrDefault(t => t.Id == trackId);
        if (track != null)
            _playerService.GetQueuePosition(trackId);
        
        return null;
    }
}