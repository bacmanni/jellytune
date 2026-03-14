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
    /// Load description for currently active artist
    /// </summary>
    public async Task OpenAsync(Guid artistId)
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
}