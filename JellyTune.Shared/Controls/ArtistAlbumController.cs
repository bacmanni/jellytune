using JellyTune.Shared.Events;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;

namespace JellyTune.Shared.Controls;

public class ArtistAlbumController
{
    private readonly IJellyTuneApiService _jellyTuneApiService;
    private readonly IFileService _fileService;
    
    public IFileService FileService => _fileService;

    public List<Album> Albums { get; private set; } = [];
    
    public event EventHandler<ArtistAlbumArgs> OnAlbumsChanged;
    
    public ArtistAlbumController(IJellyTuneApiService jellyTuneApiService, IFileService fileService)
    {
        _jellyTuneApiService = jellyTuneApiService;
        _fileService = fileService;
    }

    /// <summary>
    /// Load description for currently active artist
    /// </summary>
    public async Task OpenAsync(Guid albumId)
    {
        OnAlbumsChanged.Invoke(this, new ArtistAlbumArgs() { IsLoading = true });
        Albums = await _jellyTuneApiService.GetArtistAlbumsAsync(albumId, false);
        OnAlbumsChanged.Invoke(this, new ArtistAlbumArgs());
    }
}