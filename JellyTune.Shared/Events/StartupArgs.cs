using JellyTune.Shared.Enums;

namespace JellyTune.Shared.Events;

public class StartupArgs : EventArgs
{
    public StartupState  State { get; set; }
}