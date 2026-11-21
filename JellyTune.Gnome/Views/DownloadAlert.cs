using GObject;
using Gtk;
using AlertDialog = Adw.AlertDialog;

namespace JellyTune.Gnome.Views;

[Subclass<AlertDialog>(qualifiedName: "JellyTuneDownloadAlert")]
[Template<AssemblyResource>("JellyTune.Gnome.Blueprints.download_alert.ui")]
public partial class DownloadAlert
{
    public static DownloadAlert NewWithValues()
    {
        var obj = NewWithProperties([]);
        return obj;
    }
}