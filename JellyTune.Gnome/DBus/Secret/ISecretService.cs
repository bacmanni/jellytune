using Tmds.DBus;

namespace JellyTune.Gnome.DBus.Secret;

[DBusInterface("org.freedesktop.Secret.Service")]
public interface ISecretService : IDBusObject
{
    Task<(object Session, ObjectPath Path)> OpenSessionAsync(string algorithm, object input);
    Task<ObjectPath[]> SearchItemsAsync(IDictionary<string, string> properties);
    Task<(ObjectPath Collection, ObjectPath Prompt)> CreateCollectionAsync(IDictionary<string, object> properties, string alias);
}
