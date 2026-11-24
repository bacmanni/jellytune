using GObject;
using Gtk;
using JellyTune.Shared.Services;
using AlertDialog = Adw.AlertDialog;

namespace JellyTune.Gnome.Views;

[Subclass<AlertDialog>(qualifiedName: "JellyTuneDownloadProgressAlert")]
[Template<AssemblyResource>("JellyTune.Gnome.Blueprints.download_progress_alert.ui")]
public partial class DownloadProgressAlert
{
    public static DownloadProgressAlert NewWithValues()
    {
        var obj = NewWithProperties([]);
        return obj;
    }
}