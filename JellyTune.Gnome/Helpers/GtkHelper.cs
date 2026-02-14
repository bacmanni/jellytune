using System.Reflection;

namespace JellyTune.Gnome.Helpers;

public abstract class GtkHelper
{
    public static Gtk.Builder BuilderFromFile(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("JellyTune.Gnome.Blueprints." + name + ".ui");
        using var reader = new StreamReader(stream!);
        var uiContents = reader.ReadToEnd();

        try
        {
            return Gtk.Builder.NewFromString(uiContents, -1);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public static void GtkDispatch(Action action)
    {
        GLib.MainContext.Default().InvokeFull(0, () =>
        {
            action();
            return false;
        });
    }
}