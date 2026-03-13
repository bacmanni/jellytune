using GLib;

namespace JellyTune.Gnome.Helpers;

public class SecretHelper
{
    private static Secret.Collection GetLoginCollection()
    {
        var service = Secret.Service.GetSync(Secret.ServiceFlags.LoadCollections, null);
        var collections = service.GetCollections();

        if (collections == null)
            throw new Exception("No collections found");

        Secret.Collection? loginCollection = null;

        GLib.List.Foreach(collections, data =>
        {
            var col = (Secret.Collection)GObject.Internal.InstanceWrapper.WrapHandle<Secret.Collection>(data, false);

            if (col.GName == "org.freedesktop.secrets")
                loginCollection = col;
        });

        if (loginCollection == null)
            throw new Exception("Login collection not found");

        return loginCollection;
    }
    public static void SetPassword(string password)
    {
        // Load collections
        var login = GetLoginCollection();
        
        GLib.List.Foreach(login.GetItems(), data =>
        {
            
        });
        /*
        // Create attributes
        var attrs = new GLib.HashTable<string, string>(
            HashFunc., EqualFunc.String
        );

        attrs.Insert("username", "myuser");
        attrs.Insert("service", "myapp");

        // Create the secret value
        var secret = new Secret.Value("mypassword", "text/plain");

        // Create the item
        var item = Secret.Item.Create(
            loginCollection,
            "MyApp Password",   // label
            attrs,
            secret,
            Secret.ItemCreateFlags.Replace,
            null                // cancellable
        );

        Console.WriteLine("Secret created: " + item.Label);*/
    }
}