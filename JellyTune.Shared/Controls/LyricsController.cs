using JellyTune.Shared.Models;
using JellyTune.Shared.Services;

namespace JellyTune.Shared.Controls;

public sealed class LyricsController
{
    private readonly IJellyTuneApiService _jellyTuneApiService;
    private readonly IPlayerService _playerService;
    
    private Guid TrackId { get; set; }
    
    /// <summary>
    /// Artist name for the track
    /// </summary>
    public string ArtistName { get; private set; }
    
    /// <summary>
    /// Currently selected track name
    /// </summary>
    public string TrackName { get; private set; }
    
    /// <summary>
    /// Currently loaded lyrics
    /// </summary>
    public string? Lyrics { get; private set; }
    
    /// <summary>
    /// Loaded track album art
    /// </summary>
    public byte[]? AlbumArt { get; private set; }
    
    /// <summary>
    /// Called when lyrics data is updated
    /// </summary>
    public event EventHandler<EventArgs> OnLyricsUpdated;
    
    
    public LyricsController(IJellyTuneApiService jellyTuneApiService, IPlayerService playerService)
    {
        _jellyTuneApiService = jellyTuneApiService;
        _playerService = playerService;
    }

    public async Task Update()
    {
        var track = _playerService.GetSelectedTrack();
        if (track == null)
            return;
        
        var album = _playerService.GetSelectedAlbum();
        if (album == null)
            return;
        
        ArtistName = album.Artist;
        TrackName  = track.Name;
        AlbumArt = _playerService.GetArtwork();
        
        Lyrics = await _jellyTuneApiService.GetTrackLyricsAsync(track.Id); 
        OnLyricsUpdated.Invoke(this, EventArgs.Empty);
    }
}