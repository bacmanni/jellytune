using JellyTune.Shared.Models;

namespace JellyTune.Shared.Events;

public class AccountArgs : EventArgs
{
    public bool Validate {  get; set; }
    public Configuration Configuration { get; set; }
}