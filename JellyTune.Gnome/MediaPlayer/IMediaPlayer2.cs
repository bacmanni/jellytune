using Tmds.DBus;

namespace JellyTune.Gnome.MediaPlayer;

[DBusInterface("org.mpris.MediaPlayer2")]
public interface IMediaPlayer2 : IDBusObject
{
    Task<IDictionary<string, object>> GetAllAsync();
}