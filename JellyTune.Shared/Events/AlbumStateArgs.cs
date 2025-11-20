namespace JellyTune.Shared.Events;

public class AlbumStateArgs : EventArgs
{
    public bool UpdateAlbum { get; set; }
    public bool UpdateTracks { get; set; }
    public bool UpdateTrackState { get; set; }
    public bool UpdateArtwork { get; set; }
    public Guid? SelectedTrackId { get; set; }
}