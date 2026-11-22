using GObject;
using Gtk;
using JellyTune.Shared.Services;
using AlertDialog = Adw.AlertDialog;

namespace JellyTune.Gnome.Views;

[Subclass<AlertDialog>(qualifiedName: "JellyTuneDownloadProgressAlert")]
[Template<AssemblyResource>("JellyTune.Gnome.Blueprints.download_progress_alert.ui")]
public partial class DownloadProgressAlert
{
    private IDownloadService _downloadService;
    
    [Connect] private ProgressBar _downloadProgress;
    
    public static DownloadProgressAlert NewWithValues(IDownloadService downloadService)
    {
        var obj = NewWithProperties([]);
        obj._downloadService = downloadService;
        obj._downloadService.OnDownloadProgressChanged += (sender, args) =>
        {
            if (!args.ParentId.HasValue)
                obj.ForceClose();
                
            if (!args.TotalTrackCount.HasValue || !args.TrackNumber.HasValue) return;

            var progress = args.TrackNumber.Value;
            var totalProgress = args.TotalTrackCount.Value;
            
            var currentProgress = progress / (double)totalProgress;
            obj._downloadProgress.SetFraction(currentProgress);
            obj._downloadProgress.SetText($"Downloading: {progress}/{totalProgress}");
        };
        return obj;
    }
}