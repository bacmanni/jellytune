using System.IO.Abstractions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using JellyTune.Shared.Models;

namespace JellyTune.Shared.Services;

public class ConfigurationService(IFileSystem _fileSystem, string applicationId) : IConfigurationService
{
    private readonly Configuration _configuration = new();

    /// <summary>
    /// Occurs when the configuration object is saved
    /// </summary>
    public event EventHandler<EventArgs>? Saved;

    /// <summary>
    /// Occurs when the configuration object is loaded
    /// </summary>
    public event EventHandler<EventArgs>? Loaded;

    /// <summary>
    /// Saves the configuration file
    /// </summary>
    public void Save()
    {
        var filename = GetFilename();
        var json = JsonSerializer.Serialize(_configuration,  options: new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) });
        
        _fileSystem.File.WriteAllText(filename, json);
        Saved?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Load configuration from file
    /// </summary>
    public void Load()
    {
        var filename = GetFilename();
        if (!_fileSystem.File.Exists(filename))
        {
            CreateConfigurationFile(filename);
        }
        
        var json = _fileSystem.File.ReadAllText(filename);
        
        if (!string.IsNullOrEmpty(json))
        {
            var configuration = JsonSerializer.Deserialize<Configuration>(json);

            if (configuration != null)
            {
                var properties = typeof(Configuration).GetProperties();
                foreach (var property in properties)
                {
                    property.SetValue(_configuration, property.GetValue(configuration));
                }
            }
        }

        Loaded?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Get application configuration file directory
    /// </summary>
    /// <returns></returns>
    public string GetConfigurationDirectory()
    {
        var platform = GetOsPlatform();
        if (platform == OSPlatform.Linux)
        {
            var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (string.IsNullOrEmpty(configHome))
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                configHome = Path.Combine(home, ".config", applicationId);
            }

            return configHome;
        }
        else if (platform == OSPlatform.OSX)
        {
            return $"/Users/{Environment.UserName}/.jellytune";
        }
        
        throw new PlatformNotSupportedException();
    }

    /// <summary>
    /// Get application cache directory
    /// </summary>
    /// <returns></returns>
    public string GetCacheDirectory()
    {
        var platform = GetOsPlatform();
        if (platform == OSPlatform.Linux)
        {
            var configHome = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
            if (string.IsNullOrEmpty(configHome))
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                configHome = Path.Combine(home, ".cache", applicationId);
            }

            return configHome;
        }
        else if (platform == OSPlatform.OSX)
        {
            return $"/Users/{Environment.UserName}/.jellytune";
        }
        
        throw new PlatformNotSupportedException();
    }
    
    /// <summary>
    /// Get stored configuration
    /// </summary>
    /// <returns></returns>
    public Configuration Get()
    {
        return _configuration;
    }

    /// <summary>
    /// Is currently running platform
    /// </summary>
    /// <param name="platform"></param>
    /// <returns></returns>
    public bool IsPlatform(OSPlatform platform)
    {
        return platform == GetOsPlatform();
    }

    /// <summary>
    /// Update configuration
    /// </summary>
    /// <param name="configuration"></param>
    public void Set(Configuration configuration)
    {
        var properties = typeof(Configuration).GetProperties();
        foreach (var property in properties)
        {
            property.SetValue(_configuration, property.GetValue(configuration));
        }
    }

    /// <summary>
    /// Get latest changes from CHANGES-file
    /// </summary>
    /// <returns></returns>
    public string[] GetLatestChanges()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("JellyTune.Shared.Resources.CHANGES");
        using var reader = new StreamReader(stream!); 
        var lines = reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var changes = ParseChanges(lines);

        var latest = changes.FirstOrDefault()?.Changes.ToArray();
        return latest ?? [];
    }

    private List<Change> ParseChanges(string[] changes)
    {
        var result =  new List<Change>();
        
        foreach (var changeLine in changes)
        {
            if (string.IsNullOrWhiteSpace(changeLine)) continue;
            
            // Version and date
            if (changeLine.StartsWith("+"))
            {
                var parts = changeLine.TrimStart('+').Split(';', 2);
                
                var version = parts[0].Trim();
                var date = parts.Length > 1 ? DateTime.Parse(parts[1].Trim()) : DateTime.MinValue;
 
                result.Add(new Change() { Version =  version, Date = date });
            }
            else
            {
                if (result.Count == 0) continue;

                var change = changeLine.TrimStart('-').Trim();
                result[result.Count-1].Changes.Add(change);
            }
        }
        
        return result.OrderBy(x => x.Date).ToList();
    }
    
    private void CreateConfigurationFile(string filename)
    {
        try
        {
            var dir = _fileSystem.Path.GetDirectoryName(filename);
            if (!_fileSystem.Directory.Exists(dir))
                _fileSystem.Directory.CreateDirectory(dir);

            if (!_fileSystem.File.Exists(filename))
            {
                _fileSystem.File.CreateText(filename).Close();
                _configuration.DeviceId = Guid.NewGuid().ToString();
                Save();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    private string GetFilename()
    {
        var platform = GetOsPlatform();
        if (platform == OSPlatform.Linux)
        {
            return $"{GetConfigurationDirectory()}/configuration.json";
        }
        else if (platform == OSPlatform.OSX)
        {
            return $"{GetConfigurationDirectory()}/configuration.json";
        }
        else if (platform == OSPlatform.Windows)
        {
            return $"{GetConfigurationDirectory()}/configuration.json";
        }
        
        throw new PlatformNotSupportedException();
    }
    private OSPlatform GetOsPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return OSPlatform.Windows;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return OSPlatform.Linux;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return OSPlatform.OSX;

        throw new Exception("Unsupported OS Platform");
    }
}