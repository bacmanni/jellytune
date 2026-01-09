using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;

namespace JellyTune.Shared.Controls;

public sealed class AccountController
{
    private readonly IJellyTuneApiService _jellyTuneApiService;
    private readonly IConfigurationService _configurationService;
    private bool _isValid { get; set; }
    
    public bool IsValid() => _isValid;
    public string ServerUrl { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public bool RememberPassword { get; set; }
    public Guid? CollectionId { get; set; }
    public Guid? PlaylistCollectionId { get; set; }

    public event EventHandler<AccountArgs> OnConfigurationLoaded;

    public event EventHandler<bool> OnUpdate;
    
    public AccountController(IConfigurationService configurationService, IJellyTuneApiService jellyTuneApiService)
    {
        _jellyTuneApiService = jellyTuneApiService;
        _configurationService = configurationService;
    }

    /// <summary>
    /// Check if server is valid jellyfin server
    /// </summary>
    /// <param name="serverUrl"></param>
    /// <returns></returns>
    public async Task<bool> IsValidServerAsync(string serverUrl)
    {
        if (Uri.IsWellFormedUriString(serverUrl, UriKind.Absolute))
        {
            return await _jellyTuneApiService.CheckServerAsync(serverUrl);
        }
        
        return false;
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
        var id= _configurationService.Get()?.CollectionId;
        if (!Guid.TryParse(id, out var collectionId))
            return null;
        
        return collectionId;
    }

    /// <summary>
    /// Open input configuration
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="validate">Should validate when opened</param>
    public void OpenConfiguration(Configuration configuration, bool validate)
    {
        _isValid = true;
        ServerUrl = configuration.ServerUrl;
        Username = configuration.Username;
        Password = configuration.Password;
        RememberPassword = configuration.RememberPassword;
        
        if (!string.IsNullOrWhiteSpace(configuration.CollectionId))
            CollectionId = Guid.Parse(configuration.CollectionId);
        
        if (configuration.PlaylistCollectionId != null)
            PlaylistCollectionId = Guid.Parse(configuration.PlaylistCollectionId);
        
        OnConfigurationLoaded?.Invoke(this, new AccountArgs() {Validate = validate, Configuration = configuration });
    }

    /// <summary>
    /// Update controller validity
    /// </summary>
    /// <param name="server"></param>
    /// <param name="account"></param>
    /// <param name="collection"></param>
    public void UpdateValidity(bool server, bool account, bool collection)
    {
        if (server && account && collection)
        {
            _isValid = true;
        }
        else
        {
            _isValid = false;
        }
        
        OnUpdate?.Invoke(this, _isValid);
    }

    /// <summary>
    /// Check if account values are changed
    /// </summary>
    /// <returns></returns>
    public bool HasChanges()
    {
        var configuration = _configurationService.Get();

        if (configuration.ServerUrl != ServerUrl)
            return true;

        if (configuration.Username != Username)
            return true;
        
        if (configuration.Password != Password)
            return true;
        
        if (configuration.CollectionId != CollectionId?.ToString())
            return true;

        if (configuration.PlaylistCollectionId != PlaylistCollectionId?.ToString())
            return true;
            
        return false;
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
}