namespace JellyTune.Shared.Models;

public class Track()
{
    public Guid Id { get; set; }
    public Guid AlbumId { get; set; }
    public string Artist { get; set; }
    public string Album { get; set; }
    public int Number { get; set; }
    public string Name { get; set; }
    public TimeSpan? RunTime { get; set; }
    public bool HasLyrics { get; set; }
    public bool HasArtwork { get; set; }
}