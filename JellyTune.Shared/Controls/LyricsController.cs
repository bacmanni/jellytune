using System.Collections.Concurrent;
using System.Timers;
using JellyTune.Shared.Events;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;
using Timer = System.Timers.Timer;

namespace JellyTune.Shared.Controls;

public sealed class LyricsController
{
    private readonly IJellyTuneApiService _jellyTuneApiService;
    private readonly IPlayerService _playerService;
    private readonly IConfigurationService _configurationService;
    
    public IPlayerService PlayerService => _playerService;
    
    public IConfigurationService ConfigurationService => _configurationService;
    
    /// <summary>
    /// Artist name for the track
    /// </summary>
    public string? ArtistName { get; private set; }
    
    /// <summary>
    /// Currently selected track name
    /// </summary>
    public string? TrackName { get; private set; }

    /// <summary>
    /// Currently loaded lyrics
    /// </summary>
    public ConcurrentBag<LyricItem> Lyrics { get; private set; } = [];
    
    /// <summary>
    /// Loaded track album art
    /// </summary>
    public byte[]? AlbumArt { get; private set; }
    
    /// <summary>
    /// Called when lyrics data is updated
    /// </summary>
    public event EventHandler<EventArgs>? OnLyricsUpdated;

    public LyricsController(IJellyTuneApiService jellyTuneApiService, IPlayerService playerService, IConfigurationService configurationService)
    {
        _jellyTuneApiService = jellyTuneApiService;
        _playerService = playerService;
        _configurationService = configurationService;
    }

    public async Task UpdateAsync()
    {
        var track = _playerService.GetSelectedTrack();
        if (track == null)
            return;
        
        var album = _playerService.GetSelectedAlbum();
        if (album == null)
            return;
        
        ArtistName = album.Artist ?? string.Empty;
        TrackName  = track.Name ?? string.Empty;
        AlbumArt = _playerService.GetArtwork();

        var lyrics = await _jellyTuneApiService.GetTrackLyricsAsync(track.Id);
        lyrics.Reverse();
        Lyrics.Clear();
        foreach (var lyric in lyrics)
        {
            Lyrics.Add(lyric);
        }
        
        OnLyricsUpdated?.Invoke(this, EventArgs.Empty);
    }
}