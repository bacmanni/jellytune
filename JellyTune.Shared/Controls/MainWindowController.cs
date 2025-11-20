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
}