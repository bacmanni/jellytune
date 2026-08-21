using Adw.Internal;
using JellyTune.Gnome.Helpers;

namespace JellyTune.Gnome.Views;

[GObject.Subclass<Adw.AlertDialog>(qualifiedName: "JellyTunePreferencesAlert")]
[Gtk.Template<Gtk.AssemblyResource>("JellyTune.Gnome.Blueprints.preferences_alert.ui")]
public partial class PreferencesAlert
{
    public PreferencesAlert()
    {
    }
}