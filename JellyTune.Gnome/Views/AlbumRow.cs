using Adw.Internal;
using JellyTune.Gnome.Helpers;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;

namespace JellyTune.Gnome.Views;


[GObject.Subclass<Adw.ActionRow>(qualifiedName: "JellyTuneAlbumRow")]
[Gtk.Template<Gtk.AssemblyResource>("JellyTune.Gnome.Blueprints.album_row.ui")]
public partial class AlbumRow
{
    private readonly IFileService _fileService;
    private readonly Album _album;

    [Gtk.Connect] private Gtk.Image _albumArt;

    public AlbumRow(IFileService fileService, Album album)
    {
        _fileService = fileService;
        _album = album;
        Activatable = true;
        CanFocus = false;
        
        SetTitle(GLib.Markup.EscapeText(album.Name));
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