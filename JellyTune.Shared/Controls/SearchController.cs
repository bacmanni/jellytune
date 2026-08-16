using JellyTune.Shared.Events;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;

namespace JellyTune.Shared.Controls;

public sealed class SearchController : IDisposable
{
    private readonly IJellyTuneApiService _jellyTuneApiService;
    private readonly IConfigurationService _configurationService;
    private readonly IPlayerService _playerService;
    private readonly IFileService  _fileService;
    
    public readonly List<Models.Search> Results = [];
    private CancellationTokenSource? _cancellationTokenSource;
    
    public IFileService FileService => _fileService;
    public event EventHandler<AlbumArgs>? OnAlbumClicked;
    public event EventHandler<SearchStateArgs>? OnSearchStateChanged;
    
    public SearchController(IJellyTuneApiService jellyTuneApiService, IConfigurationService configurationService, IPlayerService playerService, IFileService fileService)
    {
        _jellyTuneApiService = jellyTuneApiService;
        _configurationService = configurationService;
        _playerService = playerService;
        _fileService = fileService;
    }

    /// <summary>
    /// Show search startup page
    /// </summary>
    public void StartSearch()
    {
        SearchStateChanged(new SearchStateArgs() { Open = true });
    }
    
    /// <summary>
    /// Open album with id
    /// </summary>
    /// <param name="albumId"></param>
    public void OpenAlbum(Guid albumId, Guid? trackId)
    {
        OnAlbumClicked?.Invoke(this, new AlbumArgs() { AlbumId = albumId, TrackId = trackId });
    }
    
    private void SearchStateChanged(SearchStateArgs e)
    {
        OnSearchStateChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Begin searching for value
    /// </summary>
    /// <param name="value"></param>
    /// <param name="cancellationToken"></param>
    public async Task SearchAlbumsAsync(string value, CancellationToken cancellationToken = default)
    {
        try
        {
            SearchStateChanged(new SearchStateArgs() { Start = true });
        
            var searchresults = await GetSearchResultsAsync(value, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
        
            Results.Clear();
            Results.AddRange(searchresults);
            SearchStateChanged(new SearchStateArgs() { Updated = true });
        }
        catch (OperationCanceledException)
        {
            // A newer SearchAlbums call cancelled this one.
        }
    }

    private async Task<List<Search>> GetSearchResultsAsync(string value, CancellationToken token)
    {
        var results = await Task.WhenAll([
            _jellyTuneApiService.SearchAlbumAsync(value, token),
            _jellyTuneApiService.SearchArtistAlbumsAsync(value, token),
            _jellyTuneApiService.SearchTrackAsync(value, token),
        ]);

        var sortList = new List<Search>();
        foreach (var result in results)
        {
            if (result != null)
                sortList.AddRange(result);
        }
        
        // Removes duplicates and sorts
        var sorted = sortList.GroupBy(x => x.Id).Select(x => x.First()).OrderBy(s => s.Type);
        return sorted.ToList();
    }

    public void Dispose()
    {
        
    }
}