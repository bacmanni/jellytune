using GLib;

namespace JellyTune.Gnome.Helpers;

public abstract class GtkHelper
{
    public static void GtkDispatch(Action action)
    {
        MainContext.Default().InvokeFull(0, () =>
        {
            action();
            return false;
        });
    }
}