using GObject;
using JellyTune.Shared.Models;
using Object = GObject.Object;

namespace JellyTune.Gnome.Models;

[Subclass<Object>]
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
        row.Artist = album.Artist != null ? album.Artist : string.Empty;
        row.Album = album.Name != null ? album.Name : string.Empty;
        row.HasArtwork = album.HasArtwork;

        return row;
    }
}