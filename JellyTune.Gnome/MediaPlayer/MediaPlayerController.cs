using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using JellyTune.Shared.Services;
using Tmds.DBus;

namespace JellyTune.Gnome.MediaPlayer;

/// <summary>
/// This will control media player widget in menu
/// </summary>
public class MediaPlayerController : IDisposable
{
    private readonly IPlayerService _playerService;
    private readonly Connection _connection = new Connection(Address.System);
    private ConnectionInfo? _connectionInfo;
    
    public MediaPlayerController(IPlayerService playerService, string applicationId)
    {
        _playerService = playerService;
        _playerService.OnPlayerStateChanged += PlayerServiceOnOnPlayerStateChanged;
    }

    /// <summary>
    /// Start connection
    /// </summary>
    public async Task ConnectAsync()
    {
        _connectionInfo = await _connection.ConnectAsync();
    }
    
    private void PlayerServiceOnOnPlayerStateChanged(object? sender, PlayerStateArgs e)
    {
        if (_connectionInfo == null)
            return;
        
        if (e.State is PlayerState.Playing or PlayerState.Paused or PlayerState.SkipNext
            or PlayerState.SkipPrevious)
        {
            OpenPlayer();
        }
        else if (e.State is PlayerState.Stopped)
        {
            ClosePlayer();
        }
    }

    private void OpenPlayer()
    {

    }
    
    private void ClosePlayer()
    {

    }
    
    /// <summary>
    /// Creates the player
    /// </summary>
    public void Connect()
    {
        // org.mpris.MediaPlayer2.$FLATPAK_ID
        //var service = new NetworkManagerService(connection, "org.freedesktop.NetworkManager");
    }

    public void Dispose()
    {
        _connection.Dispose();
        _playerService.OnPlayerStateChanged -= PlayerServiceOnOnPlayerStateChanged;
    }
}