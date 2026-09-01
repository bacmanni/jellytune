using Adw;
using Gdk;
using GLib;
using GObject;
using Gtk;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;

namespace JellyTune.Gnome.Views;

[Subclass<ActionRow>(qualifiedName: "JellyTuneSearchRow")]
[Template<AssemblyResource>("JellyTune.Gnome.Blueprints.search_row.ui")]
public partial class SearchRow
{
    private IFileService  _fileService;
    private Search _row;
    public Guid Id  { get; set; }
    public Guid AlbumId  { get; set; }
    public SearchType Type { get; set; }
    
    [Connect] private Image _albumArt;

    public static SearchRow NewWithValues(IFileService fileService, Search row)
    {
        var obj = NewWithProperties([]);
        obj._fileService = fileService;
        obj._row = row;
        obj.Id = row.Id;
        obj.AlbumId  = row.AlbumId;
        obj.Type = row.Type;
        obj.InitializeController();
        return obj;
    }

    private void InitializeController()
    {
        Activatable = true;
        
        switch (_row.Type)
        {
            case SearchType.Album or SearchType.Artist:
                SetTitle(Markup.EscapeText(_row.AlbumName != null ? _row.AlbumName : string.Empty));
                break;
            default:
                SetTitle(Markup.EscapeText(_row.TrackName != null ? _row.TrackName : string.Empty));
                break;
        }
        
        var description = $"by {Markup.EscapeText(_row.ArtistName != null ? _row.ArtistName : string.Empty)}";
        if (_row.Type == SearchType.Track)
            description += $" on {Markup.EscapeText(_row.AlbumName != null ? _row.AlbumName : string.Empty)}";
        
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
        
        using var bytes = Bytes.New(albumArt);
        using var texture = Texture.NewFromBytes(bytes);
        _albumArt.SetFromPaintable(texture);
    }
}