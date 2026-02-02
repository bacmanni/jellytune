using JellyTune.Shared.Enums;

namespace JellyTune.Shared.Events;

public class ExtendedShow : EventArgs
{
    public ExtendedType Type { get; set; }
    public bool IsVisible { get; set; }
}