using Tmds.DBus;

namespace JellyTune.Gnome.DBus.Secret;

[DBusInterface("org.freedesktop.Secret.Alias")]
public interface ISecretAlias : IDBusObject
{
    Task<ObjectPath> ReadAsync();
}
