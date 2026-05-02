using Tmds.DBus;

namespace JellyTune.Gnome.DBus.Secret;

public struct Secret(ObjectPath session, byte[] parameters, byte[] value)
{
    public ObjectPath Session { get; set; } = session;
    public byte[] Parameters { get; set; } = parameters;
    public byte[] Value { get; set; } = value;
    public string ContentType { get; set; } = "text/plain";
}

