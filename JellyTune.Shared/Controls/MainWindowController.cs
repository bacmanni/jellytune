using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
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

    /// <summary>
    /// Application background image
    /// </summary>
    public byte[]? Background;
    
    public IConfigurationService ConfigurationService => _configurationService;
    public IJellyTuneApiService JellyTuneApiService => _jellyTuneApiService;
    public IPlayerService PlayerService => _playerService;
    public IFileService FileService => _fileService;
    public event EventHandler<EventArgs>? OnApplicationBackgroundChanged;
    private CancellationTokenSource? _applicationBackgroundCts;
    
    public MainWindowController(IJellyTuneApiService jellyTuneApiService, IConfigurationService configurationService, IPlayerService playerService, IFileService fileService, ApplicationInfo applicationInfo)
    {
        _jellyTuneApiService = jellyTuneApiService;
        _configurationService = configurationService;
        _playerService = playerService;
        _fileService = fileService;
        ApplicationInfo = applicationInfo;
        
        _playerService.OnPlayerStateChanged += PlayerServiceOnOnPlayerStateChanged;
    }

    private void PlayerServiceOnOnPlayerStateChanged(object? sender, PlayerStateArgs e)
    {
        if (e.State == PlayerState.LoadedArtwork)
        {
            _ = UpdateApplicationBackground(true);
        }
        else if (e.State == PlayerState.Stopped)
        {
            _ = UpdateApplicationBackground(false);
        }
    }

    public void Dispose()
    {
        _playerService.OnPlayerStateChanged -= PlayerServiceOnOnPlayerStateChanged;
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

    public async Task UpdateApplicationBackground(bool visible)
    {
        if (visible && ConfigurationService.Get().ShowAlbumAsBackground)
        {
            _applicationBackgroundCts?.Cancel();
            _applicationBackgroundCts?.Dispose();
        
            _applicationBackgroundCts = new CancellationTokenSource();
        
            try
            {
                var albumId = PlayerService.GetSelectedAlbum()?.Id;
                if (albumId.HasValue)
                {
                    var albumArt = await FileService.GetFileAsync(FileType.AlbumArt, albumId.Value);
                    Background = albumArt;
                    OnApplicationBackgroundChanged?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    Background = null;
                    OnApplicationBackgroundChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (OperationCanceledException)
            {
                // A newer OpenAlbum call cancelled this one.
            }
        }
        else
        {
            Background = null;
            OnApplicationBackgroundChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}