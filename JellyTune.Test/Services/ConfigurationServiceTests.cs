using System.IO.Abstractions;
using System.Text;
using System.Text.Json;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;
using Moq;

namespace JellyTune.Test.Services;

public class ConfigurationServiceTests
{
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly IConfigurationService _configurationService;

    private readonly string _applicationId;
    
    public ConfigurationServiceTests()
    {
        _applicationId = "test.application.id";
        _mockFileSystem  = new Mock<IFileSystem>();
        _configurationService = new ConfigurationService(_mockFileSystem.Object, _applicationId);
    }

    [Fact]
    public async Task Load_CreateNewConfiguration_SaveConfiguration_LoadConfiguration()
    {
        var configurationFile = $"{_configurationService.GetConfigurationDirectory()}/configuration.json";
        _mockFileSystem.Setup(repo => repo.File.Exists(configurationFile)).Returns(false);
        _mockFileSystem.Setup(repo => repo.Directory.Exists("nodirectory")).Returns(false);
        _mockFileSystem.Setup(repo => repo.Path.GetDirectoryName(configurationFile)).Returns("nodirectory");
        
        using var ms = new MemoryStream();
        await using var writer = new StreamWriter(ms, Encoding.UTF8, 1024, leaveOpen: true);
        
        _mockFileSystem.Setup(repo => repo.File.CreateText(configurationFile)).Returns(writer);
        _configurationService.Load();
        await writer.FlushAsync();
        
        // Check that device id is created
        var configuration = _configurationService.Get();
        var deviceId = Guid.Parse(configuration.DeviceId);
        Assert.NotEqual(deviceId, Guid.Empty);
        
        // Test save and load saved
        configuration.AutoRefresh = !configuration.AutoRefresh;
        _configurationService.Set(configuration);
        _configurationService.Save();
        
        var configurationResult = new Configuration();
        configurationResult.AutoRefresh = !configurationResult.AutoRefresh;
        _mockFileSystem.Setup(repo => repo.File.ReadAllText(configurationFile)).Returns(JsonSerializer.Serialize(configurationResult));
        
        var savedAutoRefresh = configuration.AutoRefresh;
        configuration.AutoRefresh = !configuration.AutoRefresh;
        _configurationService.Set(configuration);
        _configurationService.Load();

        var loadedAutoRefresh = _configurationService.Get().AutoRefresh;
        
        Assert.Equal(savedAutoRefresh, loadedAutoRefresh);
    }
}