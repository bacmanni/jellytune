using Tmds.DBus;

namespace JellyTune.Gnome.MediaPlayer;

[DBusInterface("org.mpris.MediaPlayer2.Player")]
public interface IPlayer : IDBusObject
{
    
}