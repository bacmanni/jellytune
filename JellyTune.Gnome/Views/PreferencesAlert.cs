using GObject;
using Gtk;
using AlertDialog = Adw.AlertDialog;

namespace JellyTune.Gnome.Views;

[Subclass<AlertDialog>(qualifiedName: "JellyTunePreferencesAlert")]
[Template<AssemblyResource>("JellyTune.Gnome.Blueprints.preferences_alert.ui")]
public partial class PreferencesAlert
{
    public static PreferencesAlert NewWithValues()
    {
        var obj = NewWithProperties([]);
        return obj;
    }
}