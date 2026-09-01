using Adw;
using Gdk;
using GLib;
using GObject;
using Gtk;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;

namespace JellyTune.Gnome.Views;

[Subclass<ActionRow>(qualifiedName: "JellyTuneAlbumRow")]
[Template<AssemblyResource>("JellyTune.Gnome.Blueprints.album_row.ui")]
public partial class AlbumRow
{
    private IFileService _fileService;
    private Album _album;

    [Connect] private Image _albumArt;

    public static AlbumRow NewWithValues(IFileService fileService, Album album)
    {
        var obj = NewWithProperties([]);
        obj._fileService = fileService;
        obj._album = album;
        obj.InitializeController();
        return obj;
    }

    private void InitializeController()
    {
        Activatable = true;
        CanFocus = false;
        
        SetTitle(Markup.EscapeText(_album.Name != null ? _album.Name : string.Empty));
        SetSubtitle(_album.Year.ToString() ?? string.Empty);
        _ = UpdateArtwork();
    }

    public Guid AlbumId => _album.Id;

    private async Task UpdateArtwork()
    {
        var albumArt = await _fileService.GetFileAsync(FileType.AlbumArt, _album.Id);
        if  (albumArt == null || albumArt.Length == 0)
            return;
        
        using var bytes = Bytes.New(albumArt);
        using var texture = Texture.NewFromBytes(bytes);
        _albumArt.SetFromPaintable(texture);
    }
}