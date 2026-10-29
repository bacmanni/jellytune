using JellyTune.Shared.Enums;

namespace JellyTune.Shared.Events;

public class AccountArgs : EventArgs
{
    public StartupState State { get; set; }
    public bool Validate {  get; set; }
}