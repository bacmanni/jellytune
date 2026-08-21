using GObject;
using JellyTune.Shared.Models;

namespace JellyTune.Gnome.Models;

[Subclass<GObject.Object>]
public partial class AlbumRow
{
    public Guid Id  { get; set; }
    public string Artist { get; set; }
    public string Album { get; set; }
    public bool HasArtwork { get; set; }
    
    public static AlbumRow New(Album album)
    {
        var row = NewWithProperties([]);

        row.Id = album.Id;
        row.Artist = album.Artist;
        row.Album = album.Name;
        row.HasArtwork = album.HasArtwork;

        return row;
    }
}