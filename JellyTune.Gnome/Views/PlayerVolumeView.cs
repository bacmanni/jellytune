using Gtk.Internal;
using JellyTune.Gnome.Helpers;
using JellyTune.Shared.Services;

namespace JellyTune.Gnome.Views;

public partial class PlayerVolumeView : Gtk.Button
{
    private readonly IPlayerService _playerService;
    private readonly IConfigurationService _configurationService;
    
    private PlayerVolumeView(Gtk.Builder builder) : base(
        new ButtonHandle(builder.GetPointer("_root"), false))
    {
        builder.Connect(this);
    }

    public PlayerVolumeView(IPlayerService playerService, IConfigurationService configurationService) : this(
        Blueprint.BuilderFromFile("player_volume"))
    {
        _playerService = playerService;
        _configurationService = configurationService;
        
        //SetVisible( _configurationService.Get().ShowPlayerVolume);
        UpdateVolume();
    }

    private void UpdateVolume()
    {
        var volume = _playerService.GetVolume();

        if (volume == null)
        {
            SetIconName("audio-volume-muted-symbolic");
            return;
        }
        
        SetIconName("audio-volume-high-symbolic");
        // audio-volume-low-symbolic
        // audio-volume-medium-symbolic
        // audio-volume-high-symbolic
    }
}