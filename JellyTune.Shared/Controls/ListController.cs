using JellyTune.Shared.Events;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;

namespace JellyTune.Shared.Controls;

public class ListController
{
    private readonly IJellyTuneApiService _jellyTuneApiService;
    private readonly IConfigurationService _configurationService;
    private readonly IPlayerService _playerService;
    private readonly IFileService _fileService;

    public IFileService GetFileService() => _fileService;
    
    public IConfigurationService GetConfigurationService() => _configurationService;
    
    public ListController(IJellyTuneApiService jellyTuneApiService, IConfigurationService configurationService, IPlayerService playerService, IFileService fileService)
    {
        _jellyTuneApiService = jellyTuneApiService;
        _configurationService = configurationService;
        _playerService = playerService;
        _fileService = fileService;
    }
    
    private Guid? _collectionId;
    
    /// <summary>
    /// Fetched items
    /// </summary>
    private List<ListItem> Items { get;  } = [];

    /// <summary>
    /// Every time data is loaded some more, this event is fired
    /// </summary>
    public event EventHandler<ListStateArgs> OnListChanged;

    /// <summary>
    /// Called when item is clicked on the list
    /// </summary>
    public event EventHandler<Guid>? OnItemClicked;

    private void ListChanged(ListStateArgs e)
    {
        OnListChanged?.Invoke(this, e);
    }
    
    /// <summary>
    /// Item is clicked to open
    /// </summary>
    /// <param name="rowId"></param>
    public void OpenItem(Guid rowId)
    {
        OnItemClicked?.Invoke(this, rowId);
    }

    /// <summary>
    /// Set list to loading state
    /// </summary>
    /// <param name="isLoading">Is loading state on or off</param>
    public void SetLoading(bool isLoading)
    {
        ListChanged(new ListStateArgs(_collectionId, Items, isLoading));
    }

    /// <summary>
    /// Set actiuve collection
    /// </summary>
    /// <param name="collectionId"></param>
    public void SetCollectionId(Guid collectionId)
    {
        _collectionId = collectionId;
    }

    /// <summary>
    /// Get list from cache if setting is on
    /// </summary>
    public async Task GetFromCache()
    {
        if (_collectionId == null) return;
        if (_configurationService.Get().CacheListData)
        {
            var cacheList = await _fileService.GetCacheFile<List<ListItem>>($"collection-{_collectionId.Value}");
            if (cacheList == null) return;
            
            Items.Clear();
            Items.AddRange(cacheList);
            SetLoading(false);
        }
    }

    /// <summary>
    /// Clear list cache
    /// </summary>
    public void ClearCache()
    {
        if (_collectionId == null) return;
        _fileService.ClearCacheFile($"collection-{_collectionId.Value}");
    }
    
    /// <summary>
    /// Write list to cache if setting is on
    /// </summary>
    public void SetToCache()
    {
        if (_collectionId == null) return;
        if (_configurationService.Get().CacheListData)
        {
            _fileService.WriteCacheFile($"collection-{_collectionId.Value}", Items);
        }
    }
    
    /// <summary>
    /// Add items
    /// </summary>
    /// <param name="listItems"></param>
    public void AddOrUpdateItems(List<ListItem> listItems)
    {
        if (Items.Any())
        {
            Items.Clear();
            Items.AddRange(listItems);
            ListChanged(new ListStateArgs(_collectionId, Items, false) { UpdateOnly =  true });
        }
        else
        {
            Items.AddRange(listItems);
            ListChanged(new ListStateArgs(_collectionId, Items, false));
        }
    }

    /// <summary>
    /// Check if list should be fetched from the server
    /// </summary>
    /// <returns></returns>
    public bool UpdateFromServer()
    {
        if (Items.Count == 0)
            return true;

        return _configurationService.Get().AutoRefresh;
    }
    
    /// <summary>
    /// Get all items
    /// </summary>
    /// <returns></returns>
    public List<ListItem> GetItems()
    {
        return Items;
    }
    
    /// <summary>
    /// Remove all items
    /// </summary>
    public void RemoveItems()
    {
        Items.Clear();
    }
}