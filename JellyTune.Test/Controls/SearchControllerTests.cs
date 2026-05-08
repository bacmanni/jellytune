using JellyTune.Shared.Controls;
using JellyTune.Shared.Events;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;
using Moq;

namespace JellyTune.Test.Controls;

public class SearchControllerTests
{
    private readonly Mock<IJellyTuneApiService> _mockJellyTuneApiService;
    private readonly Mock<IPlayerService> _mockPlayerService;
    private readonly Mock<IConfigurationService> _mockConfigurationService;
    private readonly Mock<IFileService> _mockFileService;
    private readonly SearchController _controller;

    private readonly string _searchValue = "search";
    
    public SearchControllerTests()
    {
        _mockJellyTuneApiService =  new Mock<IJellyTuneApiService>();
        _mockPlayerService = new Mock<IPlayerService>();
        _mockConfigurationService = new Mock<IConfigurationService>();
        _mockFileService = new Mock<IFileService>();
        _controller = new SearchController(_mockJellyTuneApiService.Object, _mockConfigurationService.Object, _mockPlayerService.Object, _mockFileService.Object);

        
        var artist = new Search()
        {
            Id = Guid.NewGuid(),
            ArtistName = "Artist",
            HasArtwork = false
        };

        var album = new Search()
        {
            Id = Guid.NewGuid(),
            AlbumName =  "Album",
            HasArtwork = false
        };
        
        var track = new Search()
        {
            Id = Guid.NewGuid(),
            TrackName = "Track",
            HasArtwork = false
        };
        
        _mockJellyTuneApiService.Setup(repo => repo.SearchAlbumAsync(_searchValue, CancellationToken.None)).ReturnsAsync([album]);
        _mockJellyTuneApiService.Setup(repo => repo.SearchArtistAlbumsAsync(_searchValue, CancellationToken.None)).ReturnsAsync([artist]);
        _mockJellyTuneApiService.Setup(repo => repo.SearchTrackAsync(_searchValue, CancellationToken.None)).ReturnsAsync([track]);
    }
    
    [Fact]
    public async Task SearchAlbumsAsync()
    {
        var evt = Assert.Raises<SearchStateArgs>(handler => _controller.OnSearchStateChanged += handler, handler => _controller.OnSearchStateChanged -= handler, () => _controller.SearchAlbumsAsync(_searchValue).GetAwaiter().GetResult() );
        
        Assert.True(evt.Arguments.Updated);
        Assert.Equal(3, _controller.Results.Count);
        
        evt = Assert.Raises<SearchStateArgs>(handler => _controller.OnSearchStateChanged += handler, handler => _controller.OnSearchStateChanged -= handler, () => _controller.SearchAlbumsAsync("").GetAwaiter().GetResult() );
        Assert.True(evt.Arguments.Empty);
    }
}