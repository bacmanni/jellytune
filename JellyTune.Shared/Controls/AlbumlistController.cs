using System.Collections.Concurrent;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;

namespace JellyTune.Shared.Controls;

public class AlbumlistController : ListController, IDisposable
{
    private readonly IJellyTuneApiService _jellyTuneApiService;
    private readonly IConfigurationService _configurationService;
    private readonly IPlayerService _playerService;
    private readonly IFileService _fileService;

    /// <summary>
    /// Called when album is clicked on the list
    /// </summary>
    public event EventHandler<Guid>? OnAlbumClicked;
    
    public AlbumlistController(IJellyTuneApiService jellyTuneApiService, IConfigurationService configurationService, IPlayerService playerService, IFileService fileService) : base(jellyTuneApiService, configurationService, playerService, fileService)
    {
        _jellyTuneApiService = jellyTuneApiService;
        _configurationService = configurationService;
        _playerService = playerService;
        _fileService = fileService;
        
        OnItemClicked += ListControllerOnItemClicked;
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
            SetCollectionId(collectionId.Value);
            
            if (reload)
                ClearCache();
                
            SetLoading(true);
            RemoveItems();
            await GetFromCache();

            if (UpdateFromServer() || reload)
            {
                var albums = await _jellyTuneApiService.GetArtistsAndAlbumsAsync();
                AddOrUpdateItems(GetListItem(albums));
                SetToCache();
            }
        }
        else
        {
            RemoveItems();
            SetLoading(false);
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
        OnItemClicked -= ListControllerOnItemClicked;
    }
}