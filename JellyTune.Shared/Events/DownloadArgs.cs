namespace JellyTune.Shared.Events;

public class DownloadArgs : EventArgs
{
    public Guid? ParentId { get; set; }
    public string? TrackName { get; set; }
    public int? TrackNumber { get; set; }
    public int? TotalTrackCount { get; set; }
}
