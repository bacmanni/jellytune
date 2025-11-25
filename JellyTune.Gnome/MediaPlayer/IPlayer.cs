using Tmds.DBus;

namespace JellyTune.Gnome.MediaPlayer;

[DBusInterface("org.mpris.MediaPlayer2.Player")]
public interface IPlayer : IDBusObject
{
    Task PlayAsync();
    Task PauseAsync();
    Task PlayPauseAsync();
    Task StopAsync();
    Task NextAsync();
    Task PreviousAsync();
    Task<IDictionary<string, object>> GetAllAsync();
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
}