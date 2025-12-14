using JellyTune.Shared.Enums;

namespace JellyTune.Shared.Models;

public class ListItem
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public bool HasArtwork { get; set; }
    public FileType ArtworkFiletype { get; set; }
}