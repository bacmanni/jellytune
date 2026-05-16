using Tmds.DBus;

namespace JellyTune.Gnome.DBus.Secret;

[DBusInterface("org.freedesktop.Secret.Item")]
public interface ISecretItem : IDBusObject
{
    Task<Secret> GetSecretAsync(ObjectPath session);
    Task DeleteAsync();
    Task<T> GetAsync<T>(string property);
}