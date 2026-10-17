using JellyTune.Shared.Models;

namespace JellyTune.Shared.Events;

public class ConfigurationArgs(Configuration configuration) : EventArgs
{
    public Configuration Configuration { get; set; } = configuration;
    public readonly Dictionary<string, (object? Previous, object? Current)> Changes = new();
}