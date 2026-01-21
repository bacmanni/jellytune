namespace JellyTune.Shared.Models;

public class Change
{
    public string Version { get; set;  }
    public DateTime Date { get; set; }
    public List<string> Changes { get; set; } = [];
}