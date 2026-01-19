using JellyTune.Shared.Controls;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;
using Moq;

namespace JellyTune.Test.Controls;

public class ListControllerTests
{
    private readonly Mock<IConfigurationService> _mockConfigurationService;
    private readonly Mock<IJellyTuneApiService> _mockJellyTuneApiService;
    private readonly Mock<IPlayerService> _mockPlayerService;
    private readonly Mock<IFileService> _mockFileService;
    private readonly ListController _controller;
    
    private readonly Guid _collectionId;
    
    public ListControllerTests()
    {
        _mockJellyTuneApiService = new Mock<IJellyTuneApiService>();
        _mockConfigurationService = new Mock<IConfigurationService>();
        _mockPlayerService = new Mock<IPlayerService>();
        _mockFileService = new Mock<IFileService>();

        _collectionId = Guid.NewGuid();
        
        var configuration = new Configuration()
        {
            AutoRefresh = true,
            CacheListData = true,
        };
        
        _mockConfigurationService.Setup(repo => repo.Get()).Returns(configuration);
        
        /*
        _mockJellyTuneApiService.Setup(repo => repo.CheckServerAsync("http://test.com")).ReturnsAsync(true);
        _mockJellyTuneApiService.Setup(repo => repo.CheckServerAsync("https://test.com:8096")).ReturnsAsync(true);
        _mockJellyTuneApiService.Setup(repo => repo.LoginAsync("valid", "test")).ReturnsAsync(true);
        */
        _controller = new ListController(_mockJellyTuneApiService.Object, _mockConfigurationService.Object, _mockPlayerService.Object, _mockFileService.Object);
    }

    [Fact]
    public void UpdateFromServer()
    {
        // No items, should update
        var update = _controller.UpdateFromServer();
        Assert.True(update);

        var item = new ListItem()
        {
            Id = Guid.NewGuid(),
            Title = "Test titlet",
            Description = "Test description",
            HasArtwork = true,
            ArtworkFiletype = FileType.AlbumArt,
        };
        
        _controller.AddOrUpdateItems([item]);
        update = _controller.UpdateFromServer();
        Assert.True(update);
    }
    
    [Fact]
    public async Task GetFromCacheAsync()
    {
        var item = new ListItem()
        {
            Id = Guid.NewGuid(),
            Title = "Test titlet",
            Description = "Test description",
            HasArtwork = true,
            ArtworkFiletype = FileType.AlbumArt,
        };

        _mockFileService
            .Setup(repo => repo.GetCacheFile<List<ListItem>>($"collection-{_collectionId}", CancellationToken.None))
            .ReturnsAsync([item]);
        
        _controller.SetCollectionId(_collectionId);
        Assert.Empty(_controller.GetItems());
        
        // Should fill the list from cache
        await _controller.GetFromCacheAsync();
        Assert.NotEmpty(_controller.GetItems());
    }
}