using JellyTune.Gnome.Views;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;
using Tmds.DBus;

namespace JellyTune.Gnome.MediaPlayer;

public class MediaPlayer : IMediaPlayer2, IPlayer
{
    private readonly IFileService _fileService;
    private readonly IPlayerService _playerService;
    private readonly ApplicationInfo  _applicationInfo;
    private readonly MainWindow _mainWindow;
    private readonly Dictionary<string, object> _metadata = new();
    private readonly List<Action<PropertyChanges>> _playerSubscribers = new();

    public ObjectPath ObjectPath => new ObjectPath("/org/mpris/MediaPlayer2");

    public MediaPlayer(MainWindow mainWindow, IFileService fileService, IPlayerService playerService, ApplicationInfo applicationInfo)
    {
        _fileService = fileService;
        _playerService = playerService;
        _applicationInfo = applicationInfo;
        _mainWindow = mainWindow;
    }

    private string GetPlaybackStatus()
    {
        var state = _playerService.GetPlaybackState();
        return state switch
        {
            PlayerState.Playing => "Playing",
            PlayerState.Paused => "Paused",
            _ => "Stopped"
        };
    }

    public void UpdateMetadata()
    {
        var track = _playerService.GetSelectedTrack();
        if (track == null)
            return;
        
        _metadata["xesam:title"] = track.Name;
        _metadata["xesam:artist"] = new string[] { track.Artist };
        _metadata["xesam:album"] = track.Album;

        var url = _fileService.GetFileUrl(FileType.AlbumArt, track.AlbumId);
        if (url != null)
            _metadata["mpris:artUrl"] = url.ToString();
        
        if (track.RunTime.HasValue)
            _metadata["mpris:length"] = track.RunTime.Value.TotalMicroseconds;
        else
            _metadata["mpris:length"] = 0;
        
        var metadata = new KeyValuePair<string, object>("Metadata",_metadata);
        var status = new KeyValuePair<string, object>("PlaybackStatus", GetPlaybackStatus());
        var hasPrevious = new KeyValuePair<string, object>("CanGoPrevious", _playerService.HasPreviousTrack());
        var hasNext = new KeyValuePair<string, object>("CanGoNext", _playerService.HasNextTrack());
        NotifyPropertiesChanged(new [] { metadata, status, hasPrevious, hasNext }, Array.Empty<string>());
    }

    public Task RaiseAsync()
    {
        // Use token when PresentWithToken is found in window
        var token = Environment.GetEnvironmentVariable("XDG_ACTIVATION_TOKEN");
        
        _mainWindow.PresentWithTime(0);
        return Task.CompletedTask;
    }

    Task<IDictionary<string, object>> IMediaPlayer2.GetAllAsync()
    {
        return Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>
        {
            { "Identity", _applicationInfo.Name },
            { "DesktopEntry", _applicationInfo.Name },
            { "CanQuit", false },
            { "CanRaise", true },
            { "HasTrackList", false }
        });
    }

    Task<IDisposable> IPlayer.WatchPropertiesAsync(Action<PropertyChanges> handler)
    {
        _playerSubscribers.Add(handler);

        // Return a disposable that removes the handler when called
        return Task.FromResult<IDisposable>(new Subscription(() =>
        {
            _playerSubscribers.Remove(handler);
        }));
    }

    Task<IDictionary<string, object>> IPlayer.GetAllAsync()
    {
        return Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>
        {
            { "PlaybackStatus", GetPlaybackStatus() },
            { "Metadata", _metadata },
            { "CanPlay", true },
            { "CanPause", true },
            { "CanGoNext", true },
            { "CanGoPrevious", true }
        });
    }

    public Task PlayAsync()
    {
        return _playerService.StartTrackAsync();
    }
    
    public Task PauseAsync()
    {
        _playerService.PauseTrack();
        return Task.CompletedTask;
    }

    public Task PlayPauseAsync()
    {
        return _playerService.StartOrPauseTrackAsync();
    }

    public Task StopAsync()
    {
        _playerService.StopTrack();
        return Task.CompletedTask;
    }

    public Task NextAsync()
    {
        return _playerService.NextTrackAsync();
    }

    public Task PreviousAsync()
    {
        return _playerService.PreviousTrackAsync();
    }

    private void NotifyPropertiesChanged(KeyValuePair<string, object>[] changed, string[] invalidated)
    {
        var changes = new PropertyChanges(changed, invalidated);

        foreach (var subscriber in _playerSubscribers.ToArray())
        {
            try
            {
                subscriber(changes);
            }
            catch
            {
                // Ignore subscriber exceptions
            }
        }
    }
}