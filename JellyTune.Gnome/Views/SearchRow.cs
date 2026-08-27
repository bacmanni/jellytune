using JellyTune.Shared.Enums;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;

namespace JellyTune.Gnome.Views;

[GObject.Subclass<Adw.ActionRow>(qualifiedName: "JellyTuneSearchRow")]
[Gtk.Template<Gtk.AssemblyResource>("JellyTune.Gnome.Blueprints.search_row.ui")]
public partial class SearchRow
{
    private IFileService  _fileService;
    private Search _row;
    public Guid Id  { get; set; }
    public Guid AlbumId  { get; set; }
    public SearchType Type { get; set; }
    
    [Gtk.Connect] private Gtk.Image _albumArt;

    public static SearchRow NewWithValues(IFileService fileService, Search row)
    {
        var obj = NewWithProperties([]);
        obj._fileService = fileService;
        obj._row = row;
        obj.Id = row.Id;
        obj.AlbumId  = row.AlbumId;
        obj.InitializeController();
        return obj;
    }

    private void InitializeController()
    {
        Activatable = true;
        
        switch (_row.Type)
        {
            case SearchType.Album or SearchType.Artist:
                SetTitle(GLib.Markup.EscapeText(_row.AlbumName));
                break;
            default:
                SetTitle(GLib.Markup.EscapeText(_row.TrackName));
                break;
        }
        
        var description = $"by {GLib.Markup.EscapeText(_row.ArtistName)}";
        if (_row.Type == SearchType.Track)
            description += $" on {GLib.Markup.EscapeText(_row.AlbumName)}";
        
        SetSubtitle(description);
        
        if (!_row.HasArtwork)
            return;

        _ = UpdateArtwork();
    }
    
    private async Task UpdateArtwork()
    {
        var albumArt = await _fileService.GetFileAsync(FileType.AlbumArt, AlbumId);
        if  (albumArt == null || albumArt.Length == 0)
            return;
        
        using var bytes = GLib.Bytes.New(albumArt);
        using var texture = Gdk.Texture.NewFromBytes(bytes);
        _albumArt.SetFromPaintable(texture);
    }
}