using System.Runtime.InteropServices;
using JellyTune.Shared.Models;

namespace JellyTune.Shared.Services;

public interface IConfigurationService
{
    public event EventHandler<EventArgs>? OnSaved;
    public event EventHandler<EventArgs>? OnLoaded;
    public void Save();
    public void Load();
    public string GetConfigurationDirectory();
    public string GetCacheDirectory();
    Configuration Get();
    public bool IsPlatform(OSPlatform platform);
    public void Set(Configuration configuration);
    public string[] GetLatestChanges();
}