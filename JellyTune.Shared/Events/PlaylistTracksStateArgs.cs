namespace JellyTune.Shared.Events;

public class PlaylistTracksStateArgs : EventArgs
{
    public bool Loading { get; set; }
    public bool UpdateTrackState { get; set; }
    public Guid? SelectedTrackId { get; set; }
}