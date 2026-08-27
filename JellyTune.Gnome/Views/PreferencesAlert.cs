namespace JellyTune.Gnome.Views;

[GObject.Subclass<Adw.AlertDialog>(qualifiedName: "JellyTunePreferencesAlert")]
[Gtk.Template<Gtk.AssemblyResource>("JellyTune.Gnome.Blueprints.preferences_alert.ui")]
public partial class PreferencesAlert
{
    public static PreferencesAlert NewWithValues()
    {
        var obj = NewWithProperties([]);
        return obj;
    }
}