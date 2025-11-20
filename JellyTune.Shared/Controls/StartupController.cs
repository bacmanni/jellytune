using JellyTune.Shared.Enums;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;

namespace JellyTune.Shared.Controls;

/// <summary>
/// This controller is shared by login
/// </summary>
public sealed class StartupController : IDisposable
{
    private readonly IJellyTuneApiService _jellyTuneApiService;
    private readonly IConfigurationService _configurationService;

    public IConfigurationService GetConfigurationService() => _configurationService;
    
    public IJellyTuneApiService  GetJellyTuneApiService() => _jellyTuneApiService;
    
    public StartupController(IJellyTuneApiService jellyTuneApiService, IConfigurationService configurationService)
    {
        _jellyTuneApiService = jellyTuneApiService;
        _configurationService = configurationService;
    }

    /// <summary>
    /// This is the startup method. It will check that required data is saved.
    /// </summary>
    /// <returns></returns>
    public async Task<StartupState> StartAsync(string? nonStoredPassword = null)
    {
        var configuration = _configurationService.Get();
        
        var server = configuration.ServerUrl;
        var username = configuration.Username;
        var password = !string.IsNullOrWhiteSpace(nonStoredPassword) ? nonStoredPassword : configuration.Password;
        var collectionId = configuration.CollectionId;
        
        // This should only happen when no configuration is saved
        if (string.IsNullOrEmpty(server) && string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password))
        {
            return StartupState.InitialRun;
        }
        
        var success= _jellyTuneApiService.SetServer(server);
        if (!success)
        {
            return StartupState.InvalidServer;
        }

        var isSupportedServer = await _jellyTuneApiService.CheckServerAsync(server);
        if (!isSupportedServer)
        {
            return StartupState.InvalidServer;
        }
        
        if (string.IsNullOrEmpty(password))
        {
            return StartupState.RequirePassword;
        }
        
        var logged = await _jellyTuneApiService.LoginAsync(username, password);
        if (!logged)
        {
            return StartupState.AccountProblem;
        }
        
        var collections = await _jellyTuneApiService.GetCollectionsAsync(CollectionType.Audio);
        if (collections.Count == 0)
        {
            return StartupState.MissingCollection;
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(collectionId))
            {
                var id = Guid.Parse(collectionId);
                var collection = collections.FirstOrDefault(c => c.Id == id);
                if (collection != null)
                {
                    _jellyTuneApiService.SetCollectionId(collection.Id);
                    return StartupState.Finished;
                }
            }
            
            return StartupState.SelectCollection;
        }
    }

    /// <summary>
    /// Save configuration
    /// </summary>
    /// <param name="configuration"></param>
    public void SaveConfiguration(Configuration configuration)
    {
        _configurationService.Set(configuration);
        _configurationService.Save();
    }

    public void Dispose()
    {

    }
}