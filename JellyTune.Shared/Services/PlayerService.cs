
using System.Collections.Concurrent;
using System.Net.NetworkInformation;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using JellyTune.Shared.Models;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Providers;
using SoundFlow.Structs;
using Task = System.Threading.Tasks.Task;

namespace JellyTune.Shared.Services;

public sealed class PlayerService : IPlayerService, IDisposable
{
    private readonly IJellyTuneApiService _jellyTuneApiService;
    private readonly Configuration _configuration;

    private readonly MiniAudioEngine _engine = new();
    private readonly AudioFormat _format = AudioFormat.Dvd;
    private readonly AudioPlaybackDevice _device;
    private NetworkDataProvider? _networkDataProvider;
    private SoundPlayer? _player;
    private string _streamingUrl = string.Empty;
    private bool _networkDisconnected;
    
    /// <summary>
    /// Currently selected album
    /// </summary>
    private Album? _album { get; set; }
    
    /// <summary>
    /// Currently selected albums tracks
    /// </summary>
    private ConcurrentBag<Track> _tracks { get; } = [];
    
    /// <summary>
    /// Album artwork if found
    /// </summary>
    private byte[]? _artwork { get; set; }

    /// <summary>
    /// Currently selected track
    /// </summary>
    private Track? _selectedTrack;
    
    /// <summary>
    /// Currently started track
    /// </summary>
    private Track? _playingTrack;

    /// <summary>
    /// Currently active play session
    /// </summary>
    private string? _playSessionId;
    
    /// <summary>
    /// Event for all playing related changes
    /// </summary>
    public event EventHandler<PlayerStateArgs>? OnPlayerStateChanged;
    
    /// <summary>
    /// Updates currently playing track position
    /// This is called actively so use only if needed
    /// </summary>
    public event EventHandler<PlayerPositionArgs>? OnPlayerPositionChanged;
    
    public PlayerService(IJellyTuneApiService jellyTuneApiService)
    {
        _jellyTuneApiService = jellyTuneApiService;
        NetworkChange.NetworkAvailabilityChanged += NetworkChangeOnNetworkAvailabilityChanged;
        
        var defaultDevice = _engine.PlaybackDevices.FirstOrDefault(x => x.IsDefault);
        _device = _engine.InitializePlaybackDevice(defaultDevice, _format);
        _device.Start();
    }

    private void NetworkChangeOnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
    {
        if (!e.IsAvailable)
            if (!_networkDisconnected)
                _networkDisconnected = true;
    }

    private async Task OpenAlbumWithoutTracksAsync(Guid albumId)
    {
        PlayerStateChanged(new PlayerStateArgs(PlayerState.Loading));

        var album = await _jellyTuneApiService.GetAlbumAsync(albumId);
        _album = album ?? throw new Exception($"Album with id {albumId} not found");
        _selectedTrack = null;
        
        PlayerStateChanged(new PlayerStateArgs(PlayerState.LoadedInfo, album, _tracks.ToList()));

        if (album.HasArtwork)
        {
            _artwork = await _jellyTuneApiService.GetPrimaryArtAsync(albumId);
            PlayerStateChanged(new PlayerStateArgs(PlayerState.LoadedArtwork, album, _tracks.ToList()));
        }
    }

    private async Task OpenAlbumAsync(Guid albumId)
    {
        PlayerStateChanged(new PlayerStateArgs(PlayerState.Loading));

        _tracks.Clear();
        var album = await _jellyTuneApiService.GetAlbumAsync(albumId);
        _album = album ?? throw new Exception($"Album with id {albumId} not found");
        
        var tracks = await _jellyTuneApiService.GetTracksAsync(_album.Id);

        foreach (var track in tracks)
            _tracks.Add(track);
        
        _selectedTrack = null;

        PlayerStateChanged(new PlayerStateArgs(PlayerState.LoadedInfo, album, tracks));

        if (album.HasArtwork)
        {
            _artwork = await _jellyTuneApiService.GetPrimaryArtAsync(albumId);
            PlayerStateChanged(new PlayerStateArgs(PlayerState.LoadedArtwork, album, tracks));
        }
    }
    
    private void PlayerStateChanged(PlayerStateArgs e)
    {
        OnPlayerStateChanged?.Invoke(this, e);
    }
    
    private async Task PlayTrackAsync()
    {
        if (_selectedTrack == null)
            return;

        var trackId = _selectedTrack.Id;
        int? position = null;
        
        // Create session
        if (string.IsNullOrWhiteSpace(_playSessionId)) 
            _playSessionId = await _jellyTuneApiService.StartPlaybackAsync(trackId);
        
        // Check player status
        if (_player != null)
        {
            // Still same as selected, so we keep playing
            if (trackId == _playingTrack?.Id)
            {
                if (_networkDataProvider != null)
                {
                    position = _networkDataProvider.Position;
                }

                if (!string.IsNullOrWhiteSpace(_playSessionId))
                    _ = _jellyTuneApiService.ResumePlaybackAsync(_playSessionId, trackId, position);
                
                if (!_networkDisconnected)
                {
                    _player.Play();
                    return;
                }
                
                _networkDisconnected = false;
            }

            StopPlaying(false);    
        }
        
        _playingTrack = _tracks.FirstOrDefault(t => t.Id == trackId);

        // Get stream url and start playing
        _streamingUrl = _jellyTuneApiService.GetAudioStreamUrl(_playSessionId, trackId, position) ?? throw new Exception($"Streaming url for track with id {trackId} not found");
        _networkDataProvider = new NetworkDataProvider(_engine, _format, _streamingUrl);
        _player = new SoundPlayer(_engine, _device.Format, _networkDataProvider);
        _device.MasterMixer.AddComponent(_player);
        _player.IsLooping = false;
        _player.Play();

        _player.PlaybackEnded += async (_, args) => await OnPlaybackEnded(_, args);
    }

    private Task OnPlaybackEnded(object? sender, EventArgs args)
    {
        _ = NextTrackAsync();
        return Task.CompletedTask;
    }

    private void StopPlaying(bool endPlayback = true)
    {
        if (_playingTrack == null && _selectedTrack == null)
            return;

        var trackId = _playingTrack?.Id ?? _selectedTrack.Id;
        
        if (_player != null)
        {
            _player.PlaybackEnded -= async (_, args) => await OnPlaybackEnded(_, args);
            
            if (endPlayback && !string.IsNullOrWhiteSpace(_playSessionId))
                _jellyTuneApiService.StopPlaybackAsync(_playSessionId, trackId);
            
            _player?.Stop();
            _device.MasterMixer.RemoveComponent(_player);
            _player.Dispose();
            _networkDataProvider?.Dispose();
            _player = null;
            _playingTrack = null;
            _selectedTrack = null;
            _networkDataProvider = null;
        }
    }

    private void PausePlaying()
    {
        if (_playingTrack == null)
            return;

        var trackId = _playingTrack.Id;
        
        if (_player != null)
        {
            var position = _networkDataProvider?.Position ?? null;
            
            if (!string.IsNullOrWhiteSpace(_playSessionId))
                _jellyTuneApiService.PausePlaybackAsync(_playSessionId, trackId, position);
            
            _player.Pause();
        }
    }
    
    /// <summary>
    /// Select track from album
    /// </summary>
    /// <param name="trackId">Id of the track</param>
    public void SelectTrack(Guid trackId)
    {
        var track = _tracks.FirstOrDefault(t => t.Id == trackId);
        if (track == null) return;
        
        _selectedTrack = track;
        PlayerStateChanged(new PlayerStateArgs(PlayerState.Selected, _album, _tracks.ToList(), _selectedTrack));
    }

    /// <summary>
    /// Start playing track
    /// </summary>
    /// <param name="trackId">Id of the track. If not set uses first from the album tracks</param>
    public async Task StartTrackAsync(Guid? trackId = null)
    {
        if (!trackId.HasValue)
        {
            if (_tracks.Count > 0)
                trackId = _tracks.First().Id;
        }
        
        // Can't start anything :(
        if (!trackId.HasValue)
        {
            Console.WriteLine("Could not find track to play");
            return;
        }
        
        PlayerStateChanged(new PlayerStateArgs(PlayerState.Starting));
        var track = _tracks.FirstOrDefault(t => t.Id == trackId.Value);
        
        // Null when trying to start from album details
        if (track == null)
        {
            PlayerStateChanged(new PlayerStateArgs(PlayerState.Loading));
            track = await _jellyTuneApiService.GetTrackAsync(trackId.Value);
            await OpenAlbumAsync(track.AlbumId);
        }
        // Invalid id when trying to start from queue
        else if (track.AlbumId != _album?.Id)
        {
            await OpenAlbumWithoutTracksAsync(track.AlbumId);
        }
        
        if (_selectedTrack == null || _selectedTrack.Id != trackId.Value)
        {
            SelectTrack(trackId.Value);
        }
        
        await PlayTrackAsync();
        PlayerStateChanged(new PlayerStateArgs(PlayerState.Playing, _album, _tracks.ToList(), _selectedTrack));
    }

    /// <summary>
    /// Check if we have next track to play
    /// </summary>
    /// <returns>True, if has</returns>
    public bool HasNextTrack()
    {
        var nextTrack = _tracks.Reverse().SkipWhile(t => t != _selectedTrack).Skip(1).FirstOrDefault();
        return nextTrack != null;
    }

    /// <summary>
    /// Check if we have previous track to play
    /// </summary>
    /// <returns>True, if has</returns>
    public bool HasPreviousTrack()
    {
        var previousTrack = _tracks.SkipWhile(t => t != _selectedTrack).Skip(1).FirstOrDefault();
        return previousTrack != null;
    }

    /// <summary>
    /// Start or pause playing track
    /// </summary>
    /// <returns></returns>
    public Task StartOrPauseTrackAsync()
    {
        if (IsPlaying())
        {
            PauseTrack();
            return Task.CompletedTask;
        }
        else
        {
            return StartTrackAsync(_selectedTrack?.Id);
        }
    }

    /// <summary>
    /// Get current player state
    /// </summary>
    /// <returns></returns>
    public PlayerState GetPlaybackState()
    {
        if (IsPlaying())
            return PlayerState.Playing;
        if (IsPaused())
            return PlayerState.Paused;
        return PlayerState.Stopped;
    }

    /// <summary>
    /// Pause playing track
    /// </summary>
    public void PauseTrack()
    {
        if (_playingTrack != null)
        {
            PausePlaying();
            PlayerStateChanged(new PlayerStateArgs(PlayerState.Paused, _album, _tracks.ToList(), _selectedTrack));
        }
    }
    
    /// <summary>
    /// Stop playing started track
    /// </summary>
    public void StopTrack()
    {
        if (_playingTrack != null ||  _selectedTrack != null)
        {
            StopPlaying();
            PlayerStateChanged(new PlayerStateArgs(PlayerState.Stopped, _album, _tracks.ToList(), _selectedTrack));
        }
    }

    /// <summary>
    /// Shuffle queue
    /// </summary>
    public void ShuffleTracks()
    {
        var tracks = _tracks.ToArray();
        Random.Shared.Shuffle(tracks);
        _tracks.Clear();
        AddTracksFromPlaylist(tracks.ToList());
    }

    /// <summary>
    /// Check if playlist contains tracks
    /// </summary>
    /// <param name="countSelected">Default for checking if there are any tracks</param>
    /// <returns></returns>
    public bool HasTracks(bool countSelected = true)
    {
        if (countSelected)
        {
            return _tracks.Any();
        }

        if (_tracks.Count == 1)
            return _tracks.First().Id != _selectedTrack?.Id;
        else
            return true;
    }
    
    /// <summary>
    /// Select next track from album tracks
    /// </summary>
    public async Task NextTrackAsync()
    {
        if (_selectedTrack != null)
        {
            var isPlaying = IsPlayingTrack(_selectedTrack.Id);
            var nextTrack = _tracks.Reverse().SkipWhile(t => t != _selectedTrack).Skip(1).FirstOrDefault();

            if (nextTrack == null)
            {
                StopTrack();
                return;
            };
            
            SelectTrack(nextTrack.Id);
            PlayerStateChanged(new PlayerStateArgs(PlayerState.SkipNext, _album, _tracks.ToList(), _selectedTrack));

            if (isPlaying)
            {
                await StartTrackAsync(nextTrack.Id);
            }
        }
    }

    /// <summary>
    /// Select previous track from album tracks
    /// </summary>
    public async Task PreviousTrackAsync()
    {
        if (_selectedTrack != null)
        {
            var isPlaying = IsPlayingTrack(_selectedTrack.Id);
            var previousTrack = _tracks.SkipWhile(t => t != _selectedTrack).Skip(1).FirstOrDefault();
            
            if (previousTrack == null) return;
            
            SelectTrack(previousTrack.Id);
            PlayerStateChanged(new PlayerStateArgs(PlayerState.SkipPrevious, _album, _tracks.ToList(), _selectedTrack));
            
            if (isPlaying)
            {
                await StartTrackAsync(previousTrack.Id);
            }
        }
    }

    /// <summary>
    /// Check if track is selected or is selected with input guid
    /// </summary>
    /// <param name="trackId">Id of the track</param>
    /// <returns>True if is selected track</returns>
    public bool IsSelectedTrack(Guid? trackId)
    {
        if (!trackId.HasValue)
            return _selectedTrack != null;
        
        return _selectedTrack != null && _selectedTrack.Id == trackId;
    }
    
    /// <summary>
    /// Get currently selected track id
    /// </summary>
    /// <returns>Selected track id. Null if not found</returns>
    public Guid? GetSelectedTrackId()
    {
        return _selectedTrack?.Id;
    }

    /// <summary>
    /// Get currently selected track 
    /// </summary>
    /// <returns>Selected track. Null if not found</returns>
    public Track? GetSelectedTrack()
    {
        return _selectedTrack;
    }

    /// <summary>
    /// Get currently selected album
    /// </summary>
    /// <returns>Selected album. Null if not found</returns>
    public Album? GetSelectedAlbum()
    {
        return _album;
    }

    /// <summary>
    /// Get current play queue
    /// </summary>
    /// <returns></returns>
    public List<Track> GetTracks()
    {
        return _tracks.ToList();
    }

    /// <summary>
    /// Add single track to play queue
    /// </summary>
    /// <param name="track"></param>
    public void AddTrack(Track track)
    {
        _tracks.Add(track);

        if (!IsPlaying())
        {
            SelectTrack(track.Id);
            _ = StartTrackAsync(track.Id);
        }
    }

    /// <summary>
    /// Add more tracks to play queue
    /// </summary>
    /// <param name="playlistId"></param>
    /// <param name="tracks"></param>
    public void AddTracksFromPlaylist(List<Track> tracks)
    {
        foreach (var track in tracks)
            _tracks.Add(track);
    }

    /// <summary>
    /// Clear full queue
    /// </summary>
    public void ClearTracks()
    {
       var playingTrack = _tracks.FirstOrDefault(t => t.Id == _playingTrack?.Id);
        _tracks.Clear();

        if (playingTrack != null)
            _tracks.Add(playingTrack);
        
        // Send update with current state as previous/next track need to be updated
        var currentState = GetPlaybackState();
        PlayerStateChanged(new PlayerStateArgs(currentState, _album, _tracks.ToList(), _selectedTrack));
    }

    /// <summary>
    /// Play input track
    /// </summary>
    /// <param name="track"></param>
    public void PlayTrack(Track track)
    {
        if (_tracks.Contains(track))
        {
            _ = StartTrackAsync(track.Id);
        }
    }

    /// <summary>
    /// Get input track state
    /// </summary>
    /// <param name="trackId"></param>
    /// <returns></returns>
    public PlayerState GetTrackState(Guid trackId)
    {
        if (_playingTrack?.Id == trackId)
        {
            if (IsPlaying())
                return PlayerState.Playing;
            return PlayerState.Paused;
        }

        if (_selectedTrack?.Id == trackId)
        {
            return PlayerState.Selected;
        }
        
        return PlayerState.None;
    }

    /// <summary>
    /// Check if player is playing something
    /// </summary>
    /// <returns></returns>
    public bool IsPlaying()
    {
        return _player?.State == PlaybackState.Playing;
    }

    /// <summary>
    /// Check if player is paused
    /// </summary>
    /// <returns></returns>
    public bool IsPaused()
    {
        return _player?.State == PlaybackState.Paused;
    }
    
    /// <summary>
    /// Check if trackId is playing
    /// </summary>
    /// <param name="trackId">Id of the track. If null, then checks if any track is playing</param>
    /// <param name="albumId">Id of the album if we want to check against that too</param>
    /// <returns>True if is playing</returns>
    public bool IsPlayingTrack(Guid? trackId, Guid? albumId = null)
    {
        if (_playingTrack != null)
        {
            if (trackId.HasValue && _playingTrack.Id == trackId.Value)
            {
                // Album id has value. Check against that too
                if (albumId.HasValue)
                {
                    return _album?.Id == albumId.Value;
                }
                
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// Get number where track is in queue
    /// </summary>
    /// <param name="trackId"></param>
    /// <returns></returns>
    public int? GetQueuePosition(Guid trackId)
    {
        if (_tracks.Any(t => t.Id == trackId))
            return _tracks.ToList().FindIndex(t => t.Id == trackId);
        
        return null;
    }
    
    /// <summary>
    /// Get album artwork if available
    /// </summary>
    /// <returns>Artwork, null if none found</returns>
    public byte[]? GetArtwork()
    {
        return _artwork;
    }

    public void Dispose()
    {
        NetworkChange.NetworkAvailabilityChanged -= NetworkChangeOnNetworkAvailabilityChanged;
        
        if (_player != null)
        {
            _player.PlaybackEnded -= async (_, args) => await OnPlaybackEnded(_, args);
            _player.Stop();
            _player.Dispose();
            _player = null;
        }

        _device.Stop();
        _device.Dispose();
        _engine.Dispose();
    }
}