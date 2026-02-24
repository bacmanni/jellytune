/*
using Secret;

namespace JellyTune.Gnome.Helpers;

public class PasswordHelper
{
    private readonly string _appId = "org.bacmanni.jellytune";
    
    public static string GetPassword()
    {
        var service = Secret.Service.GetSync(Secret.ServiceFlags.LoadCollections, null);
        var collections = service.GetCollections() ?? throw new Exception("No collections found");

        
        
        Console.WriteLine("Secret collections:");
        GLib.List.Foreach(collections, data =>
        {
            var collection = (Secret.Collection) GObject.Internal.InstanceWrapper.WrapHandle<Secret.Collection>(data, false);
            if (collection.GName == "org.freedesktop.secrets")
            {
                //collection.
            }
        });





        return "";
    }
}*/