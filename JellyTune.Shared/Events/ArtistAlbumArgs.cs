namespace JellyTune.Shared.Events;

public class ArtistAlbumArgs : EventArgs
{
    public bool IsLoading { get; set; }
    public bool UpdateArtwork { get; set; }
}