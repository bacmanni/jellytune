using Adw.Internal;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;
using JellyTune.Gnome.Helpers;

namespace JellyTune.Gnome.Views;

[GObject.Subclass<Adw.ActionRow>]
[Gtk.Template<Gtk.AssemblyResource>("JellyTune.Gnome.Blueprints.search_row.ui")]
public partial class SearchRow
{
    private readonly IFileService  _fileService;
    public Guid Id  { get; set; }
    public Guid AlbumId  { get; set; }
    public SearchType Type { get; set; }
    
    [Gtk.Connect] private Gtk.Image _albumArt;

    public SearchRow(IFileService fileService, Search row)
    {
        _fileService = fileService;
        Id = row.Id;
        AlbumId  = row.AlbumId;
        
        Activatable = true;
        
        switch (row.Type)
        {
            case SearchType.Album or SearchType.Artist:
                SetTitle(GLib.Markup.EscapeText(row.AlbumName));
                break;
            default:
                SetTitle(GLib.Markup.EscapeText(row.TrackName));
                break;
        }
        
        var description = $"by {GLib.Markup.EscapeText(row.ArtistName)}";
        if (row.Type == SearchType.Track)
            description += $" on {GLib.Markup.EscapeText(row.AlbumName)}";
        
        SetSubtitle(description);
        
        if (!row.HasArtwork)
            return;

        UpdateArtwork();
    }

    private async Task UpdateArtwork()
    {
        var albumArt = await _fileService.GetFileAsync(FileType.AlbumArt, AlbumId);
        if  (albumArt == null || albumArt.Length == 0)
            return;
        
        using var bytes = GLib.Bytes.New(albumArt);
        using var texture = Gdk.Texture.NewFromBytes(bytes);
        _albumArt.SetFromPaintable(texture);
        albumArt = null;
    }
}