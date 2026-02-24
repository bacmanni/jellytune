namespace JellyTune.Shared.Events;

public class ArtistArgs : EventArgs
{
    public bool IsLoading { get; set; }
    public bool HasError { get; set; }
    public bool UpdateDetails { get; set; }
}