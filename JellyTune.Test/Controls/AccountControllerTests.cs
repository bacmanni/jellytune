using JellyTune.Shared.Controls;
using JellyTune.Shared.Services;
using Moq;

namespace JellyTune.Test.Controls;

public class AccountControllerTests
{
    private readonly Mock<IConfigurationService> _mockConfigurationService;
    private readonly Mock<IJellyTuneApiService> _mockJellyTuneApiService;
    private readonly AccountController _controller;
    
    public AccountControllerTests()
    {
        _mockJellyTuneApiService = new Mock<IJellyTuneApiService>();
        _mockConfigurationService = new Mock<IConfigurationService>();
        
        _mockJellyTuneApiService.Setup(repo => repo.CheckServerAsync("http://test.com")).ReturnsAsync(true);
        _mockJellyTuneApiService.Setup(repo => repo.CheckServerAsync("https://test.com:8096")).ReturnsAsync(true);
        _mockJellyTuneApiService.Setup(repo => repo.LoginAsync("valid", "test")).ReturnsAsync(true);
        
        _controller = new AccountController(_mockConfigurationService.Object, _mockJellyTuneApiService.Object);
    }

    [Fact]
    public async Task IsValidServerAsync()
    {
        var notValid = await _controller.IsValidServerAsync("notvalid");
        Assert.False(notValid);
        
        var valid = await _controller.IsValidServerAsync("http://test.com");
        Assert.True(valid);
        
        valid = await _controller.IsValidServerAsync("https://test.com:8096");
        Assert.True(valid);
    }
    
    [Fact]
    public async Task IsValidAccountAsync()
    {
        var invalid = await _controller.IsValidAccountAsync("invalid", "");
        Assert.False(invalid);
        
        var valid = await _controller.IsValidAccountAsync("valid", "test");
        Assert.True(valid);
    }
}