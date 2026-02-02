namespace JellyTune.Shared.Events;

public class PlayerVolumeArgs : EventArgs
{
    public float Volume { get; set; }
    public bool IsMuted { get; set; }
}