using Tmds.DBus;

namespace JellyTune.Gnome.DBus.Secret;

[DBusInterface("org.freedesktop.Secret.Collection")]
public interface ISecretCollection : IDBusObject
{
    Task<bool> GetLockedAsync();
    Task<IDictionary<string, object>> GetAllAsync();
    Task<T> GetAsync<T>(string property);
    Task<ObjectPath> CreateItemAsync(IDictionary<string, object> properties, Secret secret, bool replace);
}