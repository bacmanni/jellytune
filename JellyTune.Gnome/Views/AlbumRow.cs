using Adw.Internal;
using GLib.Internal;
using JellyTune.Gnome.Helpers;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;

namespace JellyTune.Gnome.Views;

public partial class AlbumRow : Adw.ActionRow
{
    private readonly IFileService _fileService;
    private readonly Album _album;

    [Gtk.Connect] private readonly Gtk.Image _albumArt;

    private AlbumRow(Gtk.Builder builder) : base(
        new ActionRowHandle(builder.GetPointer("_root"), false))
    {
        builder.Connect(this);
    }

    public AlbumRow(IFileService fileService, Album album) : this(
        GtkHelper.BuilderFromFile("album_row"))
    {
        _fileService = fileService;
        _album = album;
        Activatable = true;
        CanFocus = false;
        
        SetTitle(album.Name);
        SetSubtitle(album.Year.ToString() ?? string.Empty);
        _ = UpdateArtwork();
    }

    public Guid AlbumId => _album.Id;

    private async Task UpdateArtwork()
    {
        var albumArt = await _fileService.GetFileAsync(FileType.AlbumArt, _album.Id);
        if  (albumArt == null || albumArt.Length == 0)
            return;
        
        using var bytes = GLib.Bytes.New(albumArt);
        using var texture = Gdk.Texture.NewFromBytes(bytes);
        _albumArt.SetFromPaintable(texture);
        albumArt = null;
    }
}