using Gtk.Internal;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Services;
using JellyTune.Gnome.Helpers;
using JellyTune.Gnome.Models;

namespace JellyTune.Gnome.Views;

public class GridItem : Gtk.Box
{
    private readonly IFileService _fileService;
    
    [Gtk.Connect] private readonly Gtk.Image _art;
    [Gtk.Connect] private readonly Gtk.Label _title;
    [Gtk.Connect] private readonly Gtk.Label _description;
    
    private FileType _fileType;
    private CancellationTokenSource? _cancellationTokenSource;
    private Gdk.Texture? _texture;
    
    private GridItem(Gtk.Builder builder) : base(
        new BoxHandle(builder.GetPointer("_root"), false))
    {
        builder.Connect(this);
    }
    
    public GridItem(IFileService fileService) : this(Blueprint.BuilderFromFile("grid_item"))
    {
        _fileService = fileService;
        CanFocus = true;
    }

    public void Bind(ListRow row)
    {
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        _fileType = row.FileType;
        _title.SetLabel(row.Title);
        _description.SetLabel(row.Description);
        
        _art.Clear();
        _texture?.RunDispose();
        _texture = null;
        
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
        _texture = Gdk.Texture.NewFromBytes(bytes);

        if (_cancellationTokenSource is { IsCancellationRequested: true })
        {
            _texture.Dispose();
            return;
        }   
        
        _art.SetFromPaintable(_texture);
    }
    
    public void Clear()
    {
        _cancellationTokenSource?.Cancel();
        _art.Clear();
        _texture?.Dispose();
        _texture = null;
    }
    
    public override void Dispose()
    {
        Clear();
        base.Dispose();
    }
}