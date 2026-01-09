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
    
    public IFileService GetFileService() => _fileService;
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
    public async Task SearchAlbumsAsync(string value)
    {
        SearchStateChanged(new SearchStateArgs() { Start = true });
        Results.Clear();

        if (_cancellationTokenSource != null)
        {
            await _cancellationTokenSource.CancelAsync();
            _cancellationTokenSource.Dispose();
        }
        
        _cancellationTokenSource = new CancellationTokenSource();
        
        await GetSearchResultsAsync(value, _cancellationTokenSource.Token);
        
        if (_cancellationTokenSource.IsCancellationRequested) return;
        
        SearchStateChanged(new SearchStateArgs() { Updated = true });
    }

    private async Task GetSearchResultsAsync(string value, CancellationToken token)
    {
        if (token.IsCancellationRequested) return;
        
        var results = await Task.WhenAll([
            _jellyTuneApiService.SearchAlbumAsync(value),
            _jellyTuneApiService.SearchArtistAlbumsAsync(value),
            _jellyTuneApiService.SearchTrackAsync(value),
        ]);
        
        if (token.IsCancellationRequested) return;
        
        var sortList = new List<Search>();
        foreach (var result in results)
        {
            sortList.AddRange(result);
        }
        
        // Removes duplicates and sorts
        var sorted = sortList.GroupBy(x => x.Id).Select(x => x.First()).OrderBy(s => s.Type);
        Results.AddRange(sorted);
    }

    public void Dispose()
    {
        
    }
}