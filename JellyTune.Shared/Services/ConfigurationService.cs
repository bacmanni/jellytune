using System.IO.Abstractions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using JellyTune.Shared.Models;

namespace JellyTune.Shared.Services;

public class ConfigurationService(IFileSystem fileSystem, string? configurationDir, string? cacheDir) : IConfigurationService
{
    private readonly string _keySalt = "37cee24e-26a3-4a71-8e92-3bb5cecfcbc3";
    private readonly Configuration _configuration = new();

    /// <summary>
    /// Occurs when the configuration object is saved
    /// </summary>
    public event EventHandler<EventArgs>? OnSaved;

    /// <summary>
    /// Saves the configuration file
    /// </summary>
    public void Save()
    {
        var filename = GetFilename();
        
        var configuration = _configuration.ShallowCopy();
        configuration.Password = Encrypt(configuration.Password);
        
        var json = JsonSerializer.Serialize(configuration,  options: new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) });
        
        fileSystem.File.WriteAllText(filename, json);
        OnSaved?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Load configuration from file
    /// </summary>
    public void Load()
    {
        var filename = GetFilename();
        if (!fileSystem.File.Exists(filename))
        {
            CreateConfigurationFile(filename);
        }
        
        var json = fileSystem.File.ReadAllText(filename);
        
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

                try
                {
                    var decrypted = Decrypt(_configuration.Password);
                    _configuration.Password = decrypted;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Decrypting password failed: {e}");
                    _configuration.Password = null;
                }
            }
        }
    }

    private byte[] DeriveKeyFromGuid(string deviceId)
    {
        var input = Encoding.UTF8.GetBytes(deviceId);
        var salt = Encoding.UTF8.GetBytes(_keySalt);

        using var hmac = new HMACSHA256(salt);
        var keyBytes = hmac.ComputeHash(input);

        return keyBytes;
    }
    
    private string? Decrypt(string? encrypted)
    {
        if (encrypted == null) return null;

        if (_configuration.DeviceId == null) throw new Exception("Device Id missing!");
        
        var key = DeriveKeyFromGuid(_configuration.DeviceId);
        var aes = new AesGcm(key, 16);

        var combined = Convert.FromBase64String(encrypted);

        var nonce = new byte[12];
        var tag = new byte[16];
        var ciphertext = new byte[combined.Length - nonce.Length - tag.Length];

        Buffer.BlockCopy(combined, 0, nonce, 0, nonce.Length);
        Buffer.BlockCopy(combined, nonce.Length, ciphertext, 0, ciphertext.Length);
        Buffer.BlockCopy(combined, nonce.Length + ciphertext.Length, tag, 0, tag.Length);

        var plaintextBytes = new byte[ciphertext.Length];
        aes.Decrypt(nonce, ciphertext, tag, plaintextBytes);

        return Encoding.UTF8.GetString(plaintextBytes);
    }
    
    private string? Encrypt(string? password)
    {
        if (password == null) return null;
        
        if (_configuration.DeviceId == null) throw new Exception("Device Id missing!");
        
        var key = DeriveKeyFromGuid(_configuration.DeviceId);
        var aes = new AesGcm(key, 16);

        var nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);

        var plaintextBytes = Encoding.UTF8.GetBytes(password);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[16];

        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        // Combine nonce + ciphertext + tag
        var combined = new byte[nonce.Length + ciphertext.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
        Buffer.BlockCopy(ciphertext, 0, combined, nonce.Length, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, combined, nonce.Length + ciphertext.Length, tag.Length);

        return Convert.ToBase64String(combined);
    }
    
    
    
    /// <summary>
    /// Get application configuration file directory
    /// </summary>
    /// <returns></returns>
    public string? GetConfigurationDirectory()
    {
        var platform = GetOsPlatform();
        if (platform == OSPlatform.Linux)
        {
            return configurationDir;
        }

        if (platform == OSPlatform.OSX)
        {
            return $"/Users/{Environment.UserName}/.jellytune";
        }

        throw new PlatformNotSupportedException();
    }

    /// <summary>
    /// Get application cache directory
    /// </summary>
    /// <returns></returns>
    public string? GetCacheDirectory()
    {
        var platform = GetOsPlatform();
        if (platform == OSPlatform.Linux)
        {
            return cacheDir;
        }

        if (platform == OSPlatform.OSX)
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
            if (property.Name == "Password")
            {
                
            }
            
            property.SetValue(_configuration, property.GetValue(configuration));
        }
    }

    /// <summary>
    /// Set value in configuration with key
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    public void Set<T>(string key, T value)
    {
        var properties = typeof(Configuration).GetProperties();
        foreach (var property in properties)
        {
            if (property.Name == key)
            {
                property.SetValue(_configuration, value);
            }
        }
    }
    
    public T? Get<T>(string key)
    {
        var properties = typeof(Configuration).GetProperties();
        foreach (var property in properties)
        {
            if (property.Name == key)
            {
                var value = property.GetValue(_configuration);
                return value != null ? (T)value : default;
            }
        }

        return default;
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

        var latest = changes.FirstOrDefault() != null ? changes.FirstOrDefault()?.Changes.ToArray() : null;
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
 
                result.Add(new Change { Version =  version, Date = date });
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
            var dir = fileSystem.Path.GetDirectoryName(filename);
            if (dir is not null && !fileSystem.Directory.Exists(dir))
                fileSystem.Directory.CreateDirectory(dir);

            if (!fileSystem.File.Exists(filename))
            {
                fileSystem.File.CreateText(filename).Close();
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

        if (platform == OSPlatform.OSX)
        {
            return $"{GetConfigurationDirectory()}/configuration.json";
        }

        if (platform == OSPlatform.Windows)
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