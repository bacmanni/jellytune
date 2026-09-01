using System.Collections.Concurrent;
using System.Net.NetworkInformation;
using System.Timers;
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
using Timer = System.Timers.Timer;

namespace JellyTune.Shared.Services;

public sealed class PlayerService : IPlayerService, IDisposable
{
    private readonly IJellyTuneApiService _jellyTuneApiService;
    //private readonly Configuration _configuration;

    private readonly MiniAudioEngine _engine = new();
    private readonly AudioFormat _format = AudioFormat.Dvd;
    private readonly AudioPlaybackDevice? _device;
    private NetworkDataProvider? _networkDataProvider;
    private SoundPlayer? _player;
    private string _streamingUrl = string.Empty;
    private bool _networkDisconnected;
    private CancellationTokenSource? _cancellationTokenSource;
    private Timer? _playTimer;
    
    /// <summary>
    /// Currently selected album
    /// </summary>
    private Album? Album { get; set; }
    
    /// <summary>
    /// Currently selected albums tracks
    /// </summary>
    private ConcurrentBag<Track> Tracks { get; } = [];
    
    /// <summary>
    /// Album artwork if found
    /// </summary>
    private byte[]? Artwork { get; set; }

    /// <summary>
    /// Currently starting track
    /// </summary>
    private Guid? _startingTrack;
    
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

    /// <summary>
    /// Called when volume changes
    /// </summary>
    public event EventHandler<PlayerVolumeArgs>? OnPlayerVolumeChanged;

    public PlayerService(IJellyTuneApiService jellyTuneApiService)
    {
        _jellyTuneApiService = jellyTuneApiService;
        NetworkChange.NetworkAvailabilityChanged += NetworkChangeOnNetworkAvailabilityChanged;

        try
        {
            var defaultDevice = _engine.PlaybackDevices.FirstOrDefault(x => x.IsDefault);
            _device = _engine.InitializePlaybackDevice(defaultDevice, _format);
            _device.Start();
        }
        catch (Exception)
        {
            Console.WriteLine("Failed to start default audio device");
        }
    }

    private void NetworkChangeOnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
    {
        if (!e.IsAvailable)
            if (!_networkDisconnected)
                _networkDisconnected = true;
    }

    private async Task OpenAlbumWithoutTracksAsync(Guid albumId, CancellationToken cancellationToken = default)
    {
        PlayerStateChanged(new PlayerStateArgs(PlayerState.Loading));

        var album = await _jellyTuneApiService.GetAlbumAsync(albumId, cancellationToken);
        Album = album != null ? album : throw new Exception($"Album with id {albumId} not found");
        _selectedTrack = null;
        
        if (_cancellationTokenSource is { IsCancellationRequested: true })
            return;
        
        PlayerStateChanged(new PlayerStateArgs(PlayerState.LoadedInfo, album, Tracks.ToList()));

        if (album.HasArtwork)
        {
            Artwork = await _jellyTuneApiService.GetPrimaryArtAsync(albumId);
            PlayerStateChanged(new PlayerStateArgs(PlayerState.LoadedArtwork, album, Tracks.ToList()));
        }
        else
        {
            Artwork = null;
            PlayerStateChanged(new PlayerStateArgs(PlayerState.LoadedArtwork, album, Tracks.ToList()));
        }
    }

    private async Task OpenAlbumAsync(Guid albumId, CancellationToken cancellationToken = default)
    {
        PlayerStateChanged(new PlayerStateArgs(PlayerState.Loading));

        Tracks.Clear();
        var album = await _jellyTuneApiService.GetAlbumAsync(albumId, cancellationToken);
        Album = album != null ? album : throw new Exception($"Album with id {albumId} not found");
        
        var tracks = await _jellyTuneApiService.GetTracksAsync(Album.Id, cancellationToken);

        foreach (var track in tracks)
            Tracks.Add(track);
        
        _selectedTrack = null;

        if (_cancellationTokenSource is { IsCancellationRequested: true })
            return;
        
        PlayerStateChanged(new PlayerStateArgs(PlayerState.LoadedInfo, album, tracks));

        if (album.HasArtwork)
        {
            Artwork = await _jellyTuneApiService.GetPrimaryArtAsync(albumId);
            
            if (_cancellationTokenSource is { IsCancellationRequested: true })
                return;
            
            PlayerStateChanged(new PlayerStateArgs(PlayerState.LoadedArtwork, album, tracks));
        }
        else
        {
            Artwork = null;
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
        {
            var existingSession = await _jellyTuneApiService.GetPlaybackAsync();
            
            if (!string.IsNullOrWhiteSpace(existingSession))
            {
                _playSessionId = existingSession;
            }
            else
            {
                _playSessionId = await _jellyTuneApiService.StartPlaybackAsync(trackId);
            }
        }
        
        // Check player status
        if (_player != null)
        {
            // Still same as selected, so we keep playing
            if (_playingTrack != null ? trackId == _playingTrack.Id : false)
            {
                if (_networkDataProvider != null)
                {
                    position = _networkDataProvider.Position;
                }

                if (!string.IsNullOrWhiteSpace(_playSessionId))
                    await _jellyTuneApiService.ResumePlaybackAsync(_playSessionId, trackId, position);
                
                if (!_networkDisconnected)
                {
                    _player.Play();
                    return;
                }
                
                _networkDisconnected = false;
            }

            StopPlaying(false);    
        }
        
        _playingTrack = Tracks.FirstOrDefault(t => t.Id == trackId);

        // Get stream url and start playing
        _streamingUrl = _jellyTuneApiService.GetAudioStreamUrl(_playSessionId ?? string.Empty, trackId, position) != null ? _jellyTuneApiService.GetAudioStreamUrl(_playSessionId ?? string.Empty, trackId, position) : throw new Exception($"Streaming url for track with id {trackId} not found");

        if (_device != null)
        {
            _networkDataProvider = new NetworkDataProvider(_engine, _format, _streamingUrl);
            _player = new SoundPlayer(_engine, _device.Format, _networkDataProvider);
            _device.MasterMixer.AddComponent(_player);
            _player.IsLooping = false;
            _player.Play();

            var muted = IsMuted();
            var volume = GetVolumePercent();
            
            OnPlayerVolumeChanged?.Invoke(this, new PlayerVolumeArgs { IsMuted = muted, Volume = volume});
            
            _player.PlaybackEnded += async (_, _) => await OnPlaybackEnded();
            _playTimer?.Close();
            _playTimer?.Dispose();
            
            _playTimer = new Timer(250);
            _playTimer.Elapsed += TimerOnElapsed;
            _playTimer.Start();
        }
    }

    private void TimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        if (_player != null && _player.State == PlaybackState.Playing)
        {
            double seconds = _player.Time;
            OnPlayerPositionChanged?.Invoke(this, new PlayerPositionArgs { Position = seconds });
        }
    }

    private Task OnPlaybackEnded()
    {
        _ = NextTrackAsync();
        return Task.CompletedTask;
    }

    private void StopPlaying(bool endPlayback = true)
    {
        if (_playingTrack == null && _selectedTrack == null)
            return;
        
        if (_player != null)
        {
            _player.PlaybackEnded -= playerOnPlaybackEnded;

            var trackId = _playingTrack?.Id ?? _selectedTrack?.Id;
            if (endPlayback && !string.IsNullOrWhiteSpace(_playSessionId) && trackId.HasValue)
                _jellyTuneApiService.StopPlaybackAsync(_playSessionId, trackId.Value);

            _player?.Stop();
            
            if (_device is not null && _player is not null)
                _device.MasterMixer.RemoveComponent(_player);
            
            _player?.Dispose();
            _networkDataProvider?.Dispose();
            _player = null;
            _networkDataProvider = null;
        }

        _playingTrack = null;

        if (endPlayback)
        {
            _startingTrack = null;
            _selectedTrack = null;
        }
    }

    private void PausePlaying()
    {
        if (_playingTrack == null)
            return;

        var trackId = _playingTrack.Id;
        
        if (_player != null)
        {
            var position = _networkDataProvider != null ? _networkDataProvider.Position : (int?)null;
            
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
        var track = Tracks.FirstOrDefault(t => t.Id == trackId);
        if (track == null) return;
        
        _selectedTrack = track;
        PlayerStateChanged(new PlayerStateArgs(PlayerState.Selected, Album, Tracks.ToList(), _selectedTrack));
    }

    /// <summary>
    /// Start playing track
    /// </summary>
    /// <param name="trackId">Id of the track. If not set uses first from the album tracks</param>
    public async Task StartTrackAsync(Guid? trackId = null)
    {
        if (_cancellationTokenSource != null)
        {
            await _cancellationTokenSource.CancelAsync();
            _cancellationTokenSource.Dispose();
        }
        
        _cancellationTokenSource = new CancellationTokenSource();
        
        if (!trackId.HasValue)
        {
            if (Tracks.Count > 0)
                trackId = Tracks.First().Id;
        }
        
        // Can't start anything :(
        if (!trackId.HasValue)
        {
            Console.WriteLine("Could not find track to play");
            return;
        }
        
        _startingTrack = trackId.Value;
        PlayerStateChanged(new PlayerStateArgs(PlayerState.Starting) { SelectedTrackId = trackId });
        var track = Tracks.FirstOrDefault(t => t.Id == trackId.Value);
        
        // Null when trying to start from album details
        if (track == null)
        {
            PlayerStateChanged(new PlayerStateArgs(PlayerState.Loading));
            track = await _jellyTuneApiService.GetTrackAsync(trackId.Value);
            await OpenAlbumAsync(track.AlbumId);
        }
        // Invalid id when trying to start from queue
        else if (Album != null ? track.AlbumId != Album.Id : true)
        {
            await OpenAlbumWithoutTracksAsync(track.AlbumId);
        }
        
        if (_selectedTrack == null || _selectedTrack.Id != trackId.Value)
        {
            SelectTrack(trackId.Value);
        }
        
        await PlayTrackAsync();
        PlayerStateChanged(new PlayerStateArgs(PlayerState.Playing, Album, Tracks.ToList(), _selectedTrack));
    }

    /// <summary>
    /// Check if we have next track to play
    /// </summary>
    /// <returns>True, if has</returns>
    public bool HasNextTrack()
    {
        var nextTrack = Tracks.Reverse().SkipWhile(t => t != _selectedTrack).Skip(1).FirstOrDefault();
        return nextTrack != null;
    }

    /// <summary>
    /// Check if we have previous track to play
    /// </summary>
    /// <returns>True, if has</returns>
    public bool HasPreviousTrack()
    {
        var previousTrack = Tracks.SkipWhile(t => t != _selectedTrack).Skip(1).FirstOrDefault();
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

        return StartTrackAsync(_selectedTrack != null ? _selectedTrack.Id : null);
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
    /// Get volume of player. Null if muted
    /// </summary>
    /// <returns></returns>
    public double GetVolume()
    {
        return _player != null ? _player.Volume : 0;
    }

    /// <summary>
    /// Get volume percent 0-100
    /// </summary>
    /// <returns></returns>
    public int GetVolumePercent()
    {
        var volume = GetVolume();
        return (int)Math.Round(volume * 100);
    }

    /// <summary>
    /// Set volume for player
    /// </summary>
    /// <param name="volume"></param>
    public void SetVolume(double volume)
    {
        if (_player == null) return;

        _player.Volume = (float)volume;
        OnPlayerVolumeChanged?.Invoke(this, new PlayerVolumeArgs { Volume = _player.Volume, IsMuted = _player.Mute });
    }

    /// <summary>
    /// Set volume percent
    /// </summary>
    /// <param name="volume"></param>
    public void SetVolumePercent(double volume)
    {
        SetVolume(volume / 100);
    }

    /// <summary>
    /// Is player muted
    /// </summary>
    /// <returns></returns>
    public bool IsMuted()
    {
        return _player != null ? _player.Mute : false;
    }

    /// <summary>
    /// Set player mute state
    /// </summary>
    /// <param name="muted"></param>
    public void SetMuted(bool muted)
    {
        if (_player == null) return;
        
        _player.Mute = muted;
        
        OnPlayerVolumeChanged?.Invoke(this, new PlayerVolumeArgs { Volume = _player.Volume, IsMuted = _player.Mute });
    }

    /// <summary>
    /// Skip playing track backwards
    /// </summary>
    /// <param name="seconds"></param>
    public void Back(int seconds)
    {
        if (_player == null) return;
        
        double current = _player.Time;
        
        SeekTrack(current - seconds);
    }

    /// <summary>
    /// Skip playing track forwards
    /// </summary>
    /// <param name="seconds"></param>
    public void Skip(int seconds)
    {
        if (_player == null) return;
        
        double current = _player.Time;
        
        SeekTrack(current + seconds);
    }

    /// <summary>
    /// Pause playing track
    /// </summary>
    public void PauseTrack()
    {
        if (_playingTrack != null)
        {
            PausePlaying();
            PlayerStateChanged(new PlayerStateArgs(PlayerState.Paused, Album, Tracks.ToList(), _selectedTrack));
        }
    }

    /// <summary>
    /// Seek currently playing/stopped track
    /// </summary>
    /// <param name="seconds"></param>
    public void SeekTrack(double seconds)
    {
        _player?.Seek(TimeSpan.FromSeconds(seconds));
    }

    /// <summary>
    /// Stop playing started track
    /// </summary>
    public void StopTrack()
    {
        if (_playingTrack != null ||  _selectedTrack != null)
        {
            StopPlaying();
            PlayerStateChanged(new PlayerStateArgs(PlayerState.None, Album, Tracks.ToList(), _selectedTrack));
        }
    }

    /// <summary>
    /// Shuffle queue
    /// </summary>
    public void ShuffleTracks()
    {
        var tracks = Tracks.ToArray();
        Random.Shared.Shuffle(tracks);
        Tracks.Clear();
        AddTracks(tracks.ToList());
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
            return Tracks.Any();
        }

        if (Tracks.Count == 1)
            return _selectedTrack != null ? Tracks.First().Id != _selectedTrack.Id : true;
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
            var nextTrack = Tracks.Reverse().SkipWhile(t => t != _selectedTrack).Skip(1).FirstOrDefault();

            if (nextTrack == null)
            {
                StopTrack();
                return;
            }
            
            SelectTrack(nextTrack.Id);
            PlayerStateChanged(new PlayerStateArgs(PlayerState.SkipNext, Album, Tracks.ToList(), _selectedTrack));

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
            var previousTrack = Tracks.SkipWhile(t => t != _selectedTrack).Skip(1).FirstOrDefault();
            
            if (previousTrack == null) return;
            
            SelectTrack(previousTrack.Id);
            PlayerStateChanged(new PlayerStateArgs(PlayerState.SkipPrevious, Album, Tracks.ToList(), _selectedTrack));
            
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
        return _selectedTrack != null ? _selectedTrack.Id : null;
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
        return Album;
    }

    /// <summary>
    /// Get current play queue
    /// </summary>
    /// <returns></returns>
    public List<Track> GetTracks()
    {
        return Tracks.ToList();
    }

    /// <summary>
    /// Add single track to play queue
    /// </summary>
    /// <param name="track"></param>
    public void AddTrack(Track track)
    {
        Tracks.Add(track);

        if (!IsPlaying())
        {
            SelectTrack(track.Id);
            _ = StartTrackAsync(track.Id);
        }
    }

    /// <summary>
    /// Add more tracks to play queue
    /// </summary>
    /// <param name="tracks"></param>
    public void AddTracks(List<Track> tracks)
    {
        foreach (var track in tracks)
            Tracks.Add(track);
    }

    /// <summary>
    /// Clear full queue
    /// </summary>
    public void ClearTracks()
    {
        Tracks.Clear();
    }

    /// <summary>
    /// Play input track
    /// </summary>
    /// <param name="track"></param>
    public void PlayTrack(Track track)
    {
        if (Tracks.Contains(track))
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
        if (_playingTrack != null ? _playingTrack.Id == trackId : false)
        {
            if (IsPlaying())
                return PlayerState.Playing;
            return PlayerState.Paused;
        }

        if (_selectedTrack != null ? _selectedTrack.Id == trackId : false)
        {
            return PlayerState.Selected;
        }

        if (_startingTrack == trackId)
        {
            return PlayerState.Starting;
        }
        
        return PlayerState.None;
    }

    /// <summary>
    /// Check if player is playing something
    /// </summary>
    /// <returns></returns>
    public bool IsPlaying()
    {
        return _player != null ? _player.State == PlaybackState.Playing : false;
    }

    /// <summary>
    /// Check if player is paused
    /// </summary>
    /// <returns></returns>
    public bool IsPaused()
    {
        return _player != null ? _player.State == PlaybackState.Paused : false;
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
                    return Album != null ? Album.Id == albumId : false;
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
        if (Tracks.Any(t => t.Id == trackId))
            return Tracks.ToList().FindIndex(t => t.Id == trackId);
        
        return null;
    }
    
    /// <summary>
    /// Get album artwork if available
    /// </summary>
    /// <returns>Artwork, null if none found</returns>
    public byte[]? GetArtwork()
    {
        return Artwork;
    }

    public void Dispose()
    {
        NetworkChange.NetworkAvailabilityChanged -= NetworkChangeOnNetworkAvailabilityChanged;
        
        if (_player != null)
        {
            _player.PlaybackEnded -= playerOnPlaybackEnded;
            _player.Stop();
            _player.Dispose();
            _player = null;
        }

        _playTimer?.Close();
        _playTimer?.Dispose();
        _device?.Stop();
        _device?.Dispose();
        _engine.Dispose();
    }

    private async void playerOnPlaybackEnded(object? sender, EventArgs args)
    {
        await OnPlaybackEnded();
    }
}