using JellyTune.Shared.Models;
using JellyTune.Shared.Services;

namespace JellyTune.Shared.Controls;

public sealed class QueueListController : IDisposable
{
    private readonly IPlayerService _playerService;
    private readonly IFileService _fileService;
    
    public readonly List<Track> Tracks = [];
    
    public IFileService FileService => _fileService;
    public IPlayerService PlayerService => _playerService;
    
    public event EventHandler<QueueArgs>? OnQueueUpdated;
    
    public QueueListController(IPlayerService playerService, IFileService fileService)
    {
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

        OnQueueUpdated?.Invoke(this, new QueueArgs());
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
}