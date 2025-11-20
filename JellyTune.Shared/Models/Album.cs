namespace JellyTune.Shared.Models;

public class Album()
{
    public Guid Id { get; set; }
    public string Artist { get; set; }
    public string Name { get; set; }
    public int? Year { get; set; }
    public TimeSpan? Runtime { get; set; }
    public bool HasArtwork { get; set; }
}
