using GObject;
using Gtk;
using AlertDialog = Adw.AlertDialog;

namespace JellyTune.Gnome.Views;

[Subclass<AlertDialog>(qualifiedName: "JellyTuneDownloadDeleteAlert")]
[Template<AssemblyResource>("JellyTune.Gnome.Blueprints.download_delete_alert.ui")]
public partial class DownloadDeleteAlert
{
    public static DownloadDeleteAlert NewWithValues()
    {
        var obj = NewWithProperties([]);
        return obj;
    }
}