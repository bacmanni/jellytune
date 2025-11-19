using Tmds.DBus;

namespace JellyPlayer.Gnome.MediaPlayer;

[DBusInterface("org.mpris.MediaPlayer2.Player")]
public interface IPlayer : IDBusObject
{
    
}