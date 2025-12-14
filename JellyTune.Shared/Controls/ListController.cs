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
    /// Add items
    /// </summary>
    /// <param name="collectionId"></param>
    /// <param name="listItems"></param>
    public void AddItems(Guid collectionId, List<ListItem> listItems)
    {
        Items.AddRange(listItems);
        ListChanged(new ListStateArgs(_collectionId, Items, false));
    }

    /// <summary>
    /// Remove all items
    /// </summary>
    public void RemoveItems()
    {
        Items.Clear();
    }
}