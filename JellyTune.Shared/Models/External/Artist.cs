namespace JellyTune.Shared.Models.External;

public class Artist
{
    // Musicbrainz Id
    public Guid MbId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Area { get; set; }
    public int? From { get; set; }
    public int? To { get; set; }
}