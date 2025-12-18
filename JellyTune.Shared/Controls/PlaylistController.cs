using System.Collections.Concurrent;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;

namespace JellyTune.Shared.Controls;

public class PlaylistController : IDisposable
{
    private readonly ListController _listController;
    
    private readonly IJellyTuneApiService _jellyTuneApiService;
    private readonly IConfigurationService _configurationService;
    private readonly IPlayerService _playerService;
    private readonly IFileService _fileService;

    public IFileService GetFileService() => _fileService;
    
    public ListController GetListController() => _listController;
    
    public event EventHandler<Guid> OnPlaylistClicked;

    public PlaylistController(IJellyTuneApiService jellyTuneApiService, IConfigurationService configurationService, IPlayerService playerService, IFileService fileService)
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
        OnPlaylistClicked?.Invoke(this, e);
    }

    /// <summary>
    /// Refresh playlist data
    /// </summary>
    /// <param name="reload"></param>
    public async Task Refresh(bool reload = false)
    {
        if (Guid.TryParse(_configurationService.Get().PlaylistCollectionId, out var playlistCollectionId))
        {
            _listController.SetLoading(true);
            _listController.RemoveItems();
            
            var playlists = await _jellyTuneApiService.GetPlaylistsAsync(playlistCollectionId);
            _listController.AddItems(playlistCollectionId, GetListItem(playlists));
        }
        else
        {
            _listController.RemoveItems();
            _listController.SetLoading(false);
        }
    }
    
    private List<ListItem> GetListItem(List<Playlist> playlists)
    {
        var listItems = new List<ListItem>();
        foreach (var playlist in playlists)
        {
            listItems.Add(new ListItem()
            {
                Id = playlist.Id,
                Title = playlist.Name,
                Description = $"{playlist.TrackCount} tracks, {playlist.Duration.Value.TotalHours:N1}h",
                HasArtwork = playlist.HasArtwork,
                ArtworkFiletype = FileType.Playlist
            });
        }
        
        return listItems;
    }
    
    public void Dispose()
    {
        _listController.OnItemClicked -= ListControllerOnItemClicked;
    }
}