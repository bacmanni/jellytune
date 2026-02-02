using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using JellyTune.Shared.Models;

namespace JellyTune.Shared.Services;

public interface IPlayerService 
{
    public event EventHandler<PlayerStateArgs> OnPlayerStateChanged;
    public event EventHandler<PlayerPositionArgs> OnPlayerPositionChanged;
    public event EventHandler<PlayerVolumeArgs> OnPlayerVolumeChanged;
    public void SelectTrack(Guid trackId);
    public bool IsSelectedTrack(Guid? trackId = null);
    public Task StartTrackAsync(Guid? trackId = null);
    public void StopTrack();
    public void PauseTrack();
    public void SeekTrack(double seconds);
    public bool IsPlaying();
    public bool IsPaused();
    public void ShuffleTracks();
    public bool HasTracks(bool countSelected = true);
    public Task NextTrackAsync();
    public Task PreviousTrackAsync();
    public Guid? GetSelectedTrackId();
    public bool IsPlayingTrack(Guid? trackId, Guid? albumId = null);
    byte[]? GetArtwork();
    public Track? GetSelectedTrack();
    public Album? GetSelectedAlbum();
    public List<Track> GetTracks();
    public void AddTrack(Track track);
    public int? GetQueuePosition(Guid trackId);
    public void AddTracks(List<Track> tracks);
    public void ClearTracks();
    void PlayTrack(Track track);
    public PlayerState GetTrackState(Guid trackId);
    public bool HasNextTrack();
    public bool HasPreviousTrack();
    public Task StartOrPauseTrackAsync();
    public PlayerState GetPlaybackState();
    public double GetVolume();
    public int GetVolumePercent();
    public void SetVolume(double volume);
    public void SetVolumePercent(double volume);
    public bool IsMuted();
    public void SetMuted(bool muted);
}