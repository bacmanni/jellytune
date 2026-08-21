using Gtk.Internal;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Services;
using JellyTune.Gnome.Helpers;
using JellyTune.Gnome.Models;

namespace JellyTune.Gnome.Views;

[GObject.Subclass<Gtk.Box>(qualifiedName: "JellyTuneListItem")]
[Gtk.Template<Gtk.AssemblyResource>("JellyTune.Gnome.Blueprints.list_item.ui")]
public partial class ListItem
{
    private readonly IFileService _fileService;
    
    [Gtk.Connect] private Gtk.Image _art;
    [Gtk.Connect] private Gtk.Label _description;
    [Gtk.Connect] private Gtk.Label _title;

    private FileType _fileType;
    private CancellationTokenSource? _cancellationTokenSource;

    public ListItem(IFileService fileService)
    {
        _fileService = fileService;
        CanFocus = false;
    }

    public void Bind(ListRow row)
    {
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
        
        _fileType = row.FileType;
        _description.SetLabel(row.Description);
        _title.SetLabel(row.Title);
        
        _art.Clear();

        if (!row.HasArtwork)
            return;

        _ = UpdateImage(row.Id);
    }

    private async Task UpdateImage(Guid id)
    {
        if (_cancellationTokenSource is { IsCancellationRequested: true })
        {
            return;
        }
        
        var albumArt = await _fileService.GetFileAsync(_fileType, id);
        if  (albumArt == null || albumArt.Length == 0)
            return;
        
        using var bytes = GLib.Bytes.New(albumArt);
        using var texture = Gdk.Texture.NewFromBytes(bytes);
        _art.SetFromPaintable(texture);
        albumArt = null;
    }
    
    public void Clear()
    {
        _cancellationTokenSource?.Cancel();
        _art.Clear();
    }

    public override void Dispose()
    {
        Clear();
        base.Dispose();
    }
}
