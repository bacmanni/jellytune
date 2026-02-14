using Gtk.Internal;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Services;
using JellyTune.Gnome.Helpers;
using JellyTune.Gnome.Models;

namespace JellyTune.Gnome.Views;

public class AlbumGridItem : Gtk.Box
{
    private readonly IFileService _fileService;
    
    [Gtk.Connect] private readonly Gtk.Image _albumArt;
    [Gtk.Connect] private readonly Gtk.Label _album;
    [Gtk.Connect] private readonly Gtk.Label _artist;
    
    private CancellationTokenSource? _cancellationTokenSource;
    private Gdk.Texture? _texture;
    
    private AlbumGridItem(Gtk.Builder builder) : base(
        new BoxHandle(builder.GetPointer("_root"), false))
    {
        builder.Connect(this);
    }
    
    public AlbumGridItem(IFileService fileService) : this(GtkHelper.BuilderFromFile("album_grid_item"))
    {
        _fileService = fileService;
    }

    public async Task Bind(AlbumRow row)
    {
        _artist.SetLabel(GLib.Markup.EscapeText(row.Artist));
        _album.SetLabel(GLib.Markup.EscapeText(row.Album));
        
        _albumArt.Clear();
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
        
        var albumArt = await _fileService.GetFileAsync(FileType.AlbumArt, id);
        if  (albumArt == null || albumArt.Length == 0)
            return;
        
        using var bytes = GLib.Bytes.New(albumArt);
        _texture = Gdk.Texture.NewFromBytes(bytes);

        if (_cancellationTokenSource is { IsCancellationRequested: true })
        {
            _texture.Dispose();
            return;
        }   
        
        _albumArt.SetFromPaintable(_texture);
    }
    
    public void Clear()
    {
        _cancellationTokenSource?.Cancel();
        _albumArt.Clear();
        _texture?.Dispose();
        _texture = null;
    }
    
    public override void Dispose()
    {
        Clear();
        base.Dispose();
    }
}