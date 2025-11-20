using GObject;
using Gtk;
using JellyTune.Shared.Models;

namespace JellyTune.Gnome.Models;

[Subclass<GObject.Object>]
public partial class AlbumRow
{
    public Guid Id  { get; set; }
    public string Artist { get; set; }
    public string Album { get; set; }
    public bool HasArtwork { get; set; }
    
    public AlbumRow(Album album) : this()
    {
        Id = album.Id;
        Artist = album.Artist;
        Album = album.Name;
        HasArtwork = album.HasArtwork;
    }
}