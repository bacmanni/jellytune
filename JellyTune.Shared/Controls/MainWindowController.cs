using JellyTune.Shared.Models;
using JellyTune.Shared.Services;

namespace JellyTune.Shared.Controls;

public sealed class MainWindowController : IDisposable
{
    private readonly IJellyTuneApiService _jellyTuneApiService;
    private readonly IConfigurationService _configurationService;
    private readonly IPlayerService _playerService;
    private readonly IFileService _fileService;
    public readonly ApplicationInfo ApplicationInfo;
    
    public IConfigurationService GetConfigurationService() => _configurationService;
    public IJellyTuneApiService GetJellyTuneApiService() => _jellyTuneApiService;
    public IPlayerService GetPlayerService() => _playerService;
    public IFileService GetFileService() => _fileService;

    public MainWindowController(IJellyTuneApiService jellyTuneApiService, IConfigurationService configurationService, IPlayerService playerService, IFileService fileService, ApplicationInfo  applicationInfo)
    {
        _jellyTuneApiService = jellyTuneApiService;
        _configurationService = configurationService;
        _playerService = playerService;
        _fileService = fileService;
        ApplicationInfo = applicationInfo;
    }

    public void Dispose()
    {
    }

    public (int, int)? GetWindowSize()
    {
        var configuration = _configurationService.Get();

        if (configuration is { WindowWidth: not null, WindowHeight: not null })
        {
            return (configuration.WindowWidth.Value, configuration.WindowHeight.Value);
        }

        return null;
    }
    
    public void UpdateWindowSize(int width, int height)
    {
        var configuration = _configurationService.Get();
        configuration.WindowWidth = width;
        configuration.WindowHeight = height;
        _configurationService.Set(configuration);
        _configurationService.Save();
    }

    public bool HasMultipleCollections()
    {
        var configuration = _configurationService.Get();
        return !string.IsNullOrWhiteSpace(configuration.PlaylistCollectionId);
    }
}