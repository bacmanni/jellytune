using Adw.Internal;
using JellyTune.Gnome.Helpers;

namespace JellyTune.Gnome.Views;

public class PreferencesAlert : Adw.AlertDialog
{
    private PreferencesAlert(Gtk.Builder builder) : base(
        new AlertDialogHandle(builder.GetPointer("_root"), false))
    {
        builder.Connect(this);
    }

    public PreferencesAlert() : this(Blueprint.BuilderFromFile("preferences_alert"))
    {
    }
}