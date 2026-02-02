using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using JellyTune.Shared.Services;

namespace JellyTune.Shared.Controls;

public class PlayerExtendedController
{
    private readonly IPlayerService _playerService;
    private readonly IConfigurationService _configurationService;

    private ExtendedType _activeType = ExtendedType.None;

    public IConfigurationService ConfigurationService => _configurationService;
    public IPlayerService PlayerService => _playerService;
    
    public event EventHandler<ExtendedShow>? OnShowHide;

    public PlayerExtendedController(IPlayerService playerService, IConfigurationService configurationService)
    {
        _playerService = playerService;
        _configurationService = configurationService;
    }

    public void ShowExtension(ExtendedType type)
    {
        _activeType = type;
        OnShowHide?.Invoke(this, new ExtendedShow() { IsVisible = true, Type = type });
    }

    public void CloseExtension()
    {
        _activeType = ExtendedType.None;
        OnShowHide?.Invoke(this, new ExtendedShow() { IsVisible = false });
    }

    public bool IsActive(ExtendedType type)
    {
        return _activeType == type;
    }
}