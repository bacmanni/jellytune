using JellyTune.Shared.Enums;
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
    public Artist? Artist { get; private set; }
    public List<Album> Albums { get; private set; } = [];
    private CancellationTokenSource? _cancellationTokenSource;
    public event EventHandler<ArtistAlbumArgs> OnAlbumsChanged;
    
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
        if (_cancellationTokenSource != null)
        {
            await _cancellationTokenSource.CancelAsync();
            _cancellationTokenSource.Dispose();
        }
        
        if (_cancellationTokenSource?.IsCancellationRequested == true) return;
        OnAlbumsChanged.Invoke(this, new ArtistAlbumArgs() { IsLoading = true });
        Albums = await _jellyTuneApiService.GetArtistAlbumsAsync(artistId);
        OnAlbumsChanged.Invoke(this, new ArtistAlbumArgs());
    }
    
    /// <summary>
    /// Load description for currently active artist using trackId
    /// </summary>
    /// <param name="trackId"></param>
    public async Task OpenByTrackIdAsync(Guid trackId)
    {
        if (_cancellationTokenSource != null)
        {
            await _cancellationTokenSource.CancelAsync();
            _cancellationTokenSource.Dispose();
        }
        
        if (_cancellationTokenSource?.IsCancellationRequested == true) return;
        OnAlbumsChanged.Invoke(this, new ArtistAlbumArgs() { IsLoading = true });
        var artistId = await _jellyTuneApiService.GetArtistByTrackIdAsync(trackId);

        if (!artistId.HasValue)
        {
            Albums = [];
            OnAlbumsChanged.Invoke(this, new ArtistAlbumArgs());
            return;
        }
        
        Albums = await _jellyTuneApiService.GetArtistAlbumsAsync(artistId.Value);
        OnAlbumsChanged.Invoke(this, new ArtistAlbumArgs());
    }
}