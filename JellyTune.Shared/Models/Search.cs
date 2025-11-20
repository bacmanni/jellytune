using JellyTune.Shared.Enums;

namespace JellyTune.Shared.Models;

public class Search
{
    public Guid Id { get; set; }
    public Guid AlbumId { get; set; }
    public string? ArtistName { get; set; }
    public string? AlbumName { get; set; }
    public string? TrackName { get; set; }
    public SearchType Type { get; set; }
    public bool HasArtwork { get; set; }
}