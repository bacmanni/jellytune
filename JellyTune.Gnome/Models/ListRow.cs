using GObject;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Models;

namespace JellyTune.Gnome.Models;

[Subclass<GObject.Object>]
public partial class ListRow
{
    public Guid Id  { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public bool HasArtwork { get; set; }
    public FileType FileType { get; set; }
    
    public static ListRow New(ListItem item)
    {
        var row = NewWithProperties([]);

        row.Id = item.Id;
        row.Title = item.Title;
        row.Description = item.Description;
        row.HasArtwork = item.HasArtwork;
        row.FileType = item.ArtworkFiletype;

        return row;
    }
}