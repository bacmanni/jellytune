using Adw.Internal;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;
using JellyTune.Gnome.Helpers;

namespace JellyTune.Gnome.Views;

public class SearchRow : Adw.ActionRow
{
    private readonly IFileService  _fileService;
    public Guid Id  { get; set; }
    public Guid AlbumId  { get; set; }
    public SearchType Type { get; set; }
    
    [Gtk.Connect] private readonly Gtk.Image _albumArt;

    private SearchRow(Gtk.Builder builder) : base(
        new ActionRowHandle(builder.GetPointer("_root"), false))
    {
        builder.Connect(this);
    }

    public SearchRow(IFileService fileService, Search row) : this(Blueprint.BuilderFromFile("search_row"))
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
        var artWork = await _fileService.GetFileAsync(FileType.Playlist, Id);
        if (artWork == null || artWork.Length == 0)
            return;

        using var bytes = GLib.Bytes.New(artWork);
        using var texture = Gdk.Texture.NewFromBytes(bytes);
        
        GLib.MainContext.Default().InvokeFull(0, () =>
        {
            _albumArt.SetFromPaintable(texture);
            return false;
        });
    }
}