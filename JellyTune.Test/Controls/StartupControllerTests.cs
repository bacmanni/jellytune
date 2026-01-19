using JellyTune.Shared.Controls;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;
using Moq;

namespace JellyTune.Test.Controls;

public class StartupControllerTests
{
    private readonly Mock<IConfigurationService> _mockConfigurationService;
    private readonly Mock<IJellyTuneApiService> _mockJellyTuneApiService;
    private readonly StartupController _controller;
    
    public StartupControllerTests()
    {
        _mockJellyTuneApiService = new Mock<IJellyTuneApiService>();
        _mockConfigurationService = new Mock<IConfigurationService>();

        var configuration = new Configuration()
        {
            AutoRefresh = true,
            CacheListData = true,
        };
        
        _mockConfigurationService.Setup(repo => repo.Get()).Returns(configuration);

        var collection1 = new Collection()
        {
            Id = Guid.NewGuid(),
            Name = "Audio Collection",
        };
        _mockJellyTuneApiService.Setup(repo => repo.GetCollectionsAsync(CollectionType.Audio)).ReturnsAsync([collection1]);
        
        var collection2 = new Collection()
        {
            Id = Guid.NewGuid(),
            Name = "Playlist Collection",
        };
        _mockJellyTuneApiService.Setup(repo => repo.GetCollectionsAsync(CollectionType.Playlist)).ReturnsAsync([collection2]);
        
        _controller = new StartupController(_mockJellyTuneApiService.Object, _mockConfigurationService.Object);
    }
    
    [Fact]
    public async Task StartAsync()
    {
        var state = await _controller.StartAsync();
        Assert.Equal(StartupState.InitialRun, state);
        
        // Update configuration with server
        var configuration = _mockConfigurationService.Object.Get();
        configuration.ServerUrl = "http://testserver.com";
        _mockConfigurationService.Object.Set(configuration);
        
        state = await _controller.StartAsync();
        Assert.Equal(StartupState.InvalidServer, state);
        
        // Try again, so that server returns valid server
        _mockJellyTuneApiService.Setup(repo => repo.SetServer("http://testserver.com")).Returns(true);
        _mockJellyTuneApiService.Setup(repo => repo.CheckServerAsync("http://testserver.com")).ReturnsAsync(true);

        state = await _controller.StartAsync();
        Assert.Equal(StartupState.RequirePassword, state);
        
        // Update configuration with password
        configuration = _mockConfigurationService.Object.Get();
        configuration.Password = "password";
        _mockConfigurationService.Object.Set(configuration);
        
        state = await _controller.StartAsync();
        Assert.Equal(StartupState.AccountProblem, state);
        
        // Update with username
        configuration = _mockConfigurationService.Object.Get();
        configuration.Username = "username";
        _mockConfigurationService.Object.Set(configuration);
        
        _mockJellyTuneApiService.Setup(repo => repo.LoginAsync("username", "password")).ReturnsAsync(true);
        
        state = await _controller.StartAsync();
        Assert.Equal(StartupState.SelectCollection, state);
        
        // Update with collectionId
        var audioCollections = await _mockJellyTuneApiService.Object.GetCollectionsAsync(CollectionType.Audio);
        configuration = _mockConfigurationService.Object.Get();
        configuration.CollectionId = audioCollections.First().Id.ToString();
        _mockConfigurationService.Object.Set(configuration);
        
        state = await _controller.StartAsync();
        Assert.Equal(StartupState.Finished, state);
    }
}