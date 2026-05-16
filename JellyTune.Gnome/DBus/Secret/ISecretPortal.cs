using Tmds.DBus;

namespace JellyTune.Gnome.DBus.Secret;

[DBusInterface("org.freedesktop.portal.Secret")]
public interface ISecretPortal : IDBusObject
{
    Task<ObjectPath> EncryptAsync(
        string appId,
        IDictionary<string, object> parameters,
        IDictionary<string, object> options);

    Task<ObjectPath> DecryptAsync(
        string appId,
        IDictionary<string, object> parameters,
        IDictionary<string, object> options);
}
