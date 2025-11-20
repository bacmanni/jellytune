using JellyTune.Shared.Events;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;

namespace JellyTune.Shared.Controls;

public sealed class AlbumListController : IDisposable
{
    private readonly IJellyTuneApiService _jellyTuneApiService;
    private readonly IConfigurationService _configurationService;
    private readonly IPlayerService _playerService;
    private readonly IFileService _fileService;

    public IFileService GetFileService() => _fileService;
    
    public IConfigurationService GetConfigurationService() => _configurationService;
    
    /// <summary>
    /// Start index of the album list
    /// </summary>
    private int StartIndex { get; set; } = 0;

    /// <summary>
    /// Total count to fetch data for album list
    /// </summary>
    private int Count { get; } = 300;

    /// <summary>
    /// How many calls are made when fetching album list
    /// </summary>
    private int PatchSize { get; } = 5;
    
    /// <summary>
    /// Fetched albums
    /// </summary>
    private List<Album> Albums { get;  } = [];

    /// <summary>
    /// Every time data is loaded some more, this event is fired
    /// </summary>
    public event EventHandler<AlbumListStateArgs> OnAlbumListChanged;

    /// <summary>
    /// Called when album is clicked on the list
    /// </summary>
    public event EventHandler<Guid> OnAlbumClicked;

    public AlbumListController(IJellyTuneApiService jellyTuneApiService, IConfigurationService configurationService, IPlayerService playerService, IFileService fileService)
    {
        _jellyTuneApiService = jellyTuneApiService;
        _configurationService = configurationService;
        _playerService = playerService;
        _fileService = fileService;
    }
    
    private async Task<List<Album>> GetNextPatchOfAlbumsAsync()
    {
        var albums = new List<Album>();
        var queries = new List<Task<List<Album>>>();

        for (var n = 0; n < PatchSize; n++)
        {
            queries.Add(_jellyTuneApiService.GetArtistsAndAlbumsAsync(StartIndex, Count));
            StartIndex += Count;
        }
        
        var queryResults= await Task.WhenAll(queries);
        foreach (var queryResult in queryResults)
        {
            albums.AddRange(queryResult);
        }
        
        return albums;
    }

    /// <summary>
    /// Open album
    /// </summary>
    /// <param name="albumId"></param>
    public void OpenAlbum(Guid albumId)
    {
        OnAlbumClicked.Invoke(this, albumId);
    }

    private void AlbumListChanged(AlbumListStateArgs e)
    {
        OnAlbumListChanged?.Invoke(this, e);
    }
    
    /// <summary>
    /// Fetch albums in smaller patches
    /// </summary>
    public async Task RefreshAlbums(bool reload = false)
    {
        var collectionId = _jellyTuneApiService.GetCollectionId();
        if (!collectionId.HasValue)
            return;
        
        Albums.Clear();
        StartIndex = 0;
        AlbumListChanged(new AlbumListStateArgs(collectionId.Value, null));

        var fetchNextPatch = true;
        var updatedAlbums = new List<Album>();
        while (fetchNextPatch)
        {
            var result = await GetNextPatchOfAlbumsAsync();

            if (result.Count != 0)
                updatedAlbums.AddRange(result);
            else
                fetchNextPatch = false;
        }

        updatedAlbums = updatedAlbums.Distinct().OrderBy(a => a.Name).ToList();
        Albums.Clear();
        Albums.AddRange(updatedAlbums);
        AlbumListChanged(new AlbumListStateArgs(collectionId.Value, Albums, false));
    }

    public void Dispose()
    {
        
    }
}