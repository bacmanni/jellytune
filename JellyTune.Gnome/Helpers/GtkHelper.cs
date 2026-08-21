using System.Reflection;

namespace JellyTune.Gnome.Helpers;

public abstract class GtkHelper
{
    public static void GtkDispatch(Action action)
    {
        GLib.MainContext.Default().InvokeFull(0, () =>
        {
            action();
            return false;
        });
    }
}