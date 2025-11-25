using Tmds.DBus;

namespace JellyTune.Gnome.MediaPlayer;

[DBusInterface("org.freedesktop.DBus")]
public interface IFreedesktopDbus : IDBusObject
{
    Task<uint> RequestNameAsync(string name, uint flags);
}