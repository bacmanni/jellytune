using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;
using Tmds.DBus;

namespace JellyTune.Gnome.MediaPlayer;

/// <summary>
/// This will control media player widget in menu
/// </summary>
public class MediaPlayerController : IDisposable
{
    private readonly IFileService _fileService;
    private readonly IPlayerService _playerService;
    private readonly ApplicationInfo _applicationInfo;
    private readonly Connection _connection = new Connection(Address.Session);
    private readonly string _serviceName;
    private MediaPlayer? _mediaPlayer;
    private RegisterState _registerState = RegisterState.Unregistered;
    
    public MediaPlayerController(IFileService fileService, IPlayerService playerService, ApplicationInfo applicationInfo)
    {
        _fileService = fileService;
        _playerService = playerService;
        _applicationInfo = applicationInfo;
        _serviceName = $"org.mpris.MediaPlayer2.{_applicationInfo.Id}";
        
        _playerService.OnPlayerStateChanged += PlayerServiceOnPlayerStateChanged;
    }

    private void PlayerServiceOnPlayerStateChanged(object? sender, PlayerStateArgs e)
    {
        if (e.State == PlayerState.Playing)
            _ = RegisterPlayer();
        
        if (e.State == PlayerState.Stopped)
            if (!_playerService.HasNextTrack())
                UnRegisterPlayer();

        if (e.SelectedTrack != null && _registerState == RegisterState.Registered)
            _mediaPlayer?.UpdateMetadata();
    }

    /// <summary>
    /// Start connection
    /// </summary>
    public async Task ConnectAsync()
    {
        try
        {
            await _connection.ConnectAsync();
            _mediaPlayer = new MediaPlayer(_fileService, _playerService, _applicationInfo);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private async Task RegisterPlayer()
    {
        if (_mediaPlayer == null || _registerState is RegisterState.Registering or RegisterState.Registered)
            return;
        
        _registerState = RegisterState.Registering;
        await _connection.RegisterObjectAsync(_mediaPlayer);
        var dbus = _connection.CreateProxy<IFreedesktopDbus>("org.freedesktop.DBus", "/org/freedesktop/DBus");
        await dbus.RequestNameAsync(_serviceName, 0);
        _mediaPlayer.UpdateMetadata();
        _registerState = RegisterState.Registered;
    }

    private void UnRegisterPlayer()
    {
        if (_mediaPlayer == null || _registerState != RegisterState.Registered)
            return;
        
        _connection.UnregisterObject(_mediaPlayer);
    }
    
    public void Dispose()
    {
        _playerService.OnPlayerStateChanged -= PlayerServiceOnPlayerStateChanged;
        _connection.Dispose();
    }
}