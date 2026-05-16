using JellyTune.Shared.Events;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;

namespace JellyTune.Shared.Controls;

public class AlbumArtController
{
    private readonly IJellyTuneApiService _jellyTuneApiService;
    
    public Album Album { get; private set; }
    
    public byte[]? ArtWork { get; private set; }
    
    public event EventHandler<AlbumArtArgs> OnAlbumArtChanged;
    private CancellationTokenSource? _cancellationTokenSource;
    
    public AlbumArtController(IJellyTuneApiService jellyTuneApiService)
    {
        _jellyTuneApiService = jellyTuneApiService;
    }
    
    public async Task OpenAsync(Album album)
    {
        if (_cancellationTokenSource != null)
        {
            await _cancellationTokenSource.CancelAsync();
            _cancellationTokenSource.Dispose();
        }
        
        if (_cancellationTokenSource?.IsCancellationRequested == true) return;
        
        Album = album;
        OnAlbumArtChanged.Invoke(this, new AlbumArtArgs() { IsLoading = true });
        ArtWork = await _jellyTuneApiService.GetPrimaryArtAsync(album.Id, 400);
        OnAlbumArtChanged.Invoke(this, new AlbumArtArgs());
    }
}