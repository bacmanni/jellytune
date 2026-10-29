using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;

namespace JellyTune.Shared.Controls;

public sealed class AccountController
{
    private readonly IJellyTuneApiService _jellyTuneApiService;
    private readonly IConfigurationService _configurationService;
    public IConfigurationService ConfigurationService => _configurationService;
    public bool IsValid { get; private set; }
    public string? ServerUrl { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public Guid? CollectionId { get; set; }
    public Guid? PlaylistCollectionId { get; set; }

    public event EventHandler<EventArgs>? OnConfigurationValueChanged;
    public event EventHandler<AccountArgs>? OnConfigurationLoaded;

    public AccountController(IConfigurationService configurationService, IJellyTuneApiService jellyTuneApiService)
    {
        _jellyTuneApiService = jellyTuneApiService;
        _configurationService = configurationService;
    }

    /// <summary>
    /// Check if server url is valid url
    /// </summary>
    /// <param name="serverUrl"></param>
    /// <returns></returns>
    public bool IsValidServerUrl(string serverUrl)
    {
        return Uri.IsWellFormedUriString(serverUrl, UriKind.Absolute);
    }
    
    /// <summary>
    /// Check if server is valid jellyfin server
    /// </summary>
    /// <param name="serverUrl"></param>
    /// <returns></returns>
    public async Task<bool> IsValidServerAsync(string serverUrl)
    {
        return await _jellyTuneApiService.CheckServerAsync(serverUrl);
    }

    /// <summary>
    /// Check that login was ok
    /// </summary>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    public async Task<bool> IsValidAccountAsync(string username, string password)
    {
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            return await _jellyTuneApiService.LoginAsync(username, password);
        }
        
        return false;
    }

    /// <summary>
    /// Get available collections
    /// </summary>
    /// <returns></returns>
    public async Task<List<Collection>> GetCollectionsAsync(CollectionType type)
    {
        return await _jellyTuneApiService.GetCollectionsAsync(type);
    }

    /// <summary>
    /// Get selected collection Id
    /// </summary>
    /// <returns></returns>
    public Guid? GetSelectedAudioCollectionId()
    {
        var id= _configurationService.Get().CollectionId;
        if (!Guid.TryParse(id, out var collectionId))
            return null;
        
        return collectionId;
    }

    /// <summary>
    /// Open input configuration
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="startupState"></param>
    public void OpenConfiguration(Configuration configuration, StartupState startupState)
    {
        IsValid = true;
        ServerUrl = configuration.ServerUrl;
        Username = configuration.Username;
        Password = configuration.Password;
        
        if (!string.IsNullOrWhiteSpace(configuration.CollectionId))
            CollectionId = Guid.Parse(configuration.CollectionId);
        
        if (!string.IsNullOrWhiteSpace(configuration.PlaylistCollectionId))
            PlaylistCollectionId = Guid.Parse(configuration.PlaylistCollectionId);
        
        OnConfigurationLoaded?.Invoke(this, new AccountArgs() { State = startupState} );
    }

    /// <summary>
    /// Get selected playlist collection id
    /// </summary>
    /// <returns></returns>
    public Guid? GetSelectedPlaylistCollectionId()
    {
        var id= _configurationService.Get().PlaylistCollectionId;
        if (!Guid.TryParse(id, out var collectionId))
            return null;
        
        return collectionId;
    }

    /// <summary>
    /// Set account valid status
    /// </summary>
    /// <param name="valid"></param>
    public void SetValid(bool valid)
    {
        IsValid = valid;
    }

    /// <summary>
    /// Called when some value changes
    /// </summary>
    public void ConfigurationValueChanged()
    {
        OnConfigurationValueChanged?.Invoke(this, new AccountArgs());
    }

    /// <summary>
    /// Check that required account 
    /// </summary>
    /// <returns></returns>
    public bool HasRequiredValues()
    {
        return !string.IsNullOrWhiteSpace(ServerUrl) && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
    }
}