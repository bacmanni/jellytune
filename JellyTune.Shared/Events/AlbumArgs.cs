namespace JellyTune.Shared.Events;

public class AlbumArgs : EventArgs
{
    public Guid AlbumId { get; set; }
    
    public Guid? TrackId { get; set; }
}