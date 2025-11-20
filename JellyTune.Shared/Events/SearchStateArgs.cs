namespace JellyTune.Shared.Events;

public class SearchStateArgs : EventArgs
{
    public bool Clear { get; set; }
    public bool Empty { get; set; }
    public bool Start { get; set; }
    public bool Open { get; set; }
    public bool Updated { get; set; }
}