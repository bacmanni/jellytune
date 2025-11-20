using JellyTune.Shared.Models;
using JellyTune.Shared.Services;

namespace JellyTune.Shared.Controls;

public sealed class QueueListController : IDisposable
{
    private readonly IJellyTuneApiService _jellyTuneApiService;
    private readonly IConfigurationService _configurationService;
    private readonly IPlayerService _playerService;
    private readonly IFileService _fileService;
    
    public readonly List<Models.Track> Tracks = [];
    
    public IFileService GetFileService() => _fileService;
    public IPlayerService GetPlayerService() => _playerService;
    
    public event EventHandler<QueueArgs> OnQueueUpdated;
    
    public QueueListController(IJellyTuneApiService jellyTuneApiService, IConfigurationService configurationService, IPlayerService playerService, IFileService fileService)
    {
        _jellyTuneApiService = jellyTuneApiService;
        _configurationService = configurationService;
        _playerService = playerService;
        _fileService = fileService;
    }

    /// <summary>
    /// Open current queue. Data is fetched from playerservice
    /// </summary>
    public void Open()
    {
        Tracks.Clear();
        var tracks = _playerService.GetTracks();
        tracks.Reverse();
        Tracks.AddRange(tracks);

        OnQueueUpdated.Invoke(this, new QueueArgs());
    }
    
    
    public void Dispose() 
    {

    }

    /// <summary>
    /// Randomize current queue
    /// </summary>
    public void ShuffleTracks()
    {
        _playerService.ShuffleTracks();
        Open();
    }

    /// <summary>
    /// Clear current queue
    /// </summary>
    public void ClearTracks()
    {
        _playerService.ClearTracks();
        Open();
    }
}