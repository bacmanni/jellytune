using GObject;
using Gtk;
using JellyTune.Shared.Events;
using JellyTune.Shared.Services;

namespace JellyTune.Gnome.Views;

[Subclass<Button>(qualifiedName: "JellyTuneDownloadButton")]
[Template<AssemblyResource>("JellyTune.Gnome.Blueprints.download_button.ui")]
public partial class DownloadButton
{
    private IDownloadService _downloadService;
    
    [Connect] private Stack _downloadStack;
    [Connect] private Image _downloadImage;
    [Connect] private Adw.Spinner _downloadSpinner;
    [Connect] private Popover _downloadPopover;
    [Connect] private ProgressBar _downloadProgress;
    
    private Guid _parentId;

    public event EventHandler<DownloadArgs>? OnDownloadClicked;
    
    public event EventHandler<DownloadArgs>? OnDeleteClicked;
    
    public void Initialize(IDownloadService downloadService)
    {
        _downloadService = downloadService;
        OnClicked += OnButtonClicked;
        _downloadService.OnDownloadProgressChanged += DownloadServiceOnDownloadProgressChanged;
    }
    
    private void OnButtonClicked(Button sender, EventArgs e)
    {
        // Show popover about progress
        if (_downloadService.IsDownloading(_parentId))
        {
            _downloadPopover.Popup();
            return;
        }

        // Found on device. Ask for remove
        if (_downloadService.HasParent(_parentId))
        {
            var alert = DownloadDeleteAlert.NewWithValues();
            alert.Present(this);
            alert.OnResponse += (dialog, args) =>
            {
                if (args.Response == "delete")
                {
                    OnDeleteClicked?.Invoke(this, new DownloadArgs() { ParentId = _parentId });
                }
            };
        }
        else
        {
            OnDownloadClicked?.Invoke(this, new DownloadArgs() { ParentId = _parentId });
        }
    }

    private void UpdateIcon(int? progress = null, int? totalProgress = null)
    {
        if (_downloadService.IsDownloading(_parentId))
        {
            if (progress.HasValue && totalProgress.HasValue)
            {
                var currentProgress = progress.Value / (double)totalProgress.Value;
                _downloadProgress.SetFraction(currentProgress);
                _downloadProgress.SetText($"Downloading: {progress}/{totalProgress}");
            }
            
            _downloadStack.SetVisibleChild(_downloadSpinner);
            SetTooltipText("Download progress");
        }
        else
        {
            _downloadPopover.Popdown();
            
            if (_downloadService.HasParent(_parentId))
            {
                _downloadImage.SetFromIconName("user-trash-symbolic");
                SetTooltipText("Delete tracks");
            }
            else
            {
                _downloadImage.SetFromIconName("folder-download-symbolic");
                SetTooltipText("Download tracks");
            }
            
            _downloadStack.SetVisibleChild(_downloadImage);
        }
    }
    
    private void DownloadServiceOnDownloadProgressChanged(object? sender, DownloadArgs e)
    {
        UpdateIcon(e.TrackNumber, e.TotalTrackCount);
    }

    public void UpdateSensitivity(bool sensitive)
    {
        SetSensitive(sensitive);
    }
    
    public void SetParent(Guid parent)
    {
        _parentId = parent;
        UpdateIcon();
    }
}