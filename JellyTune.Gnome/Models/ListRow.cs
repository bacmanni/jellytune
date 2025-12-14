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
    
    public ListRow(ListItem item) : this()
    {
        Id = item.Id;
        Title = item.Title;
        Description = item.Description;
        HasArtwork = item.HasArtwork;
        FileType = item.ArtworkFiletype;
    }
}