using JellyTune.Shared.Events;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;

namespace JellyTune.Shared.Controls;

public class ArtistAlbumController
{
    private readonly IJellyTuneApiService _jellyTuneApiService;
    private readonly IFileService _fileService;
    
    public IFileService FileService => _fileService;
    public byte[]? ArtWork = null;
    public List<Album> Albums { get; private set; } = [];
    private CancellationTokenSource? _openByArtistIdCts;
    private CancellationTokenSource? _openByTrackIdCts;
    public event EventHandler<ArtistAlbumArgs>? OnAlbumsChanged;
    
    public ArtistAlbumController(IJellyTuneApiService jellyTuneApiService, IFileService fileService)
    {
        _jellyTuneApiService = jellyTuneApiService;
        _fileService = fileService;
    }

    /// <summary>
    /// Load description for currently active artist using artistId
    /// </summary>
    public async Task OpenByArtistIdAsync(Guid artistId)
    {
        _openByArtistIdCts?.Cancel();
        _openByArtistIdCts?.Dispose();
        
        _openByArtistIdCts = new CancellationTokenSource();
        var cancellationToken = _openByArtistIdCts.Token;

        try
        {
            OnAlbumsChanged?.Invoke(this, new ArtistAlbumArgs { IsLoading = true });
            var albums = await _jellyTuneApiService.GetArtistAlbumsAsync(artistId, null, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            
            Albums = albums;
            OnAlbumsChanged?.Invoke(this, new ArtistAlbumArgs());
        }
        catch (OperationCanceledException)
        {
            // A newer OpenByArtistId call cancelled this one.
        }
    }

    /// <summary>
    /// Load description for currently active artist using trackId
    /// </summary>
    /// <param name="trackId"></param>
    public async Task OpenByTrackIdAsync(Guid trackId)
    {
        _openByTrackIdCts?.Cancel();
        _openByTrackIdCts?.Dispose();

        _openByTrackIdCts = new CancellationTokenSource();
        var cancellationToken = _openByTrackIdCts.Token;

        try
        {
            OnAlbumsChanged?.Invoke(this, new ArtistAlbumArgs { IsLoading = true });
            var artistId = await _jellyTuneApiService.GetArtistByTrackIdAsync(trackId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            
            if (!artistId.HasValue)
            {
                Albums = [];
                OnAlbumsChanged?.Invoke(this, new ArtistAlbumArgs());
                return;
            }
        
            var albums = await _jellyTuneApiService.GetArtistAlbumsAsync(artistId.Value, null, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            Albums = albums;
            OnAlbumsChanged?.Invoke(this, new ArtistAlbumArgs());
        }
        catch (OperationCanceledException)
        {
            // A newer OpenByTrackId call cancelled this one.
        }
    }
}