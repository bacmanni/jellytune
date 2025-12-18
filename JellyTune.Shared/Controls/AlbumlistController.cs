using System.Collections.Concurrent;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;

namespace JellyTune.Shared.Controls;

public class AlbumlistController : IDisposable
{
    private readonly ListController _listController;
    
    private readonly IJellyTuneApiService _jellyTuneApiService;
    private readonly IConfigurationService _configurationService;
    private readonly IPlayerService _playerService;
    private readonly IFileService _fileService;

    public ListController GetListController() => _listController;
    
    /// <summary>
    /// Called when album is clicked on the list
    /// </summary>
    public event EventHandler<Guid>? OnAlbumClicked;
    
    public AlbumlistController(IJellyTuneApiService jellyTuneApiService, IConfigurationService configurationService, IPlayerService playerService, IFileService fileService)
    {
        _listController = new ListController(jellyTuneApiService, configurationService, playerService, fileService);
        _jellyTuneApiService = jellyTuneApiService;
        _configurationService = configurationService;
        _playerService = playerService;
        _fileService = fileService;
        
        _listController.OnItemClicked += ListControllerOnItemClicked;
    }

    private void ListControllerOnItemClicked(object? sender, Guid e)
    {
        OnAlbumClicked?.Invoke(this,e);
    }

    /// <summary>
    /// Refresh albumlist data
    /// </summary>
    /// <param name="reload"></param>
    public async Task Refresh(bool reload = false)
    {
        var collectionId = _jellyTuneApiService.GetCollectionId();
        if (collectionId.HasValue)
        {
            _listController.SetLoading(true);
            _listController.RemoveItems();

            var albums = await _jellyTuneApiService.GetArtistsAndAlbumsAsync();
            _listController.AddItems(collectionId.Value, GetListItem(albums));
        }
        else
        {
            _listController.RemoveItems();
            _listController.SetLoading(false);
        }
    }
    
    private List<ListItem> GetListItem(List<Album> albums)
    {
        var listItems = new List<ListItem>();
        foreach (var album in albums)
        {
            listItems.Add(new ListItem()
            {
                Id = album.Id,
                Title = album.Name,
                Description = album.Artist,
                HasArtwork = album.HasArtwork,
                ArtworkFiletype = FileType.AlbumArt
            });
        }
        
        return listItems;
    }

    public void Dispose()
    {
        _listController.OnItemClicked -= ListControllerOnItemClicked;
    }
}