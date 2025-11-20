using JellyTune.Shared.Enums;

namespace JellyTune.Shared.Models;

public class Playlist
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public TimeSpan? Duration { get; set; }
    public int TrackCount { get; set; }
    public bool HasArtwork { get; set; }
}