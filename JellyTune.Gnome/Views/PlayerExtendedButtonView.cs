using JellyTune.Gnome.Helpers;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using Button = Gtk.Button;

namespace JellyTune.Gnome.Views;

[GObject.Subclass<Gtk.Box>(qualifiedName: "JellyTunePlayerExtendedButtonView")]
[Gtk.Template<Gtk.AssemblyResource>("JellyTune.Gnome.Blueprints.player_extended_button.ui")]
public partial class PlayerExtendedButtonView
{
    private PlayerExtendedController _controller;

    [Gtk.Connect] private Gtk.ToggleButton _position;
    [Gtk.Connect] private Gtk.ToggleButton _volume;

    private bool _initialized = false;
    
    public static PlayerExtendedButtonView NewWithValues(PlayerExtendedController controller)
    {
        var obj = NewWithProperties([]);
        obj._controller = controller;
        obj.InitializeController();
        return obj;
    }

    private void InitializeController()
    {
        _controller.PlayerService.OnPlayerVolumeChanged += PlayerServiceOnPlayerVolumeChanged;
        _controller.ConfigurationService.OnSaved += ConfigurationServiceOnSaved;
        
        _position.OnClicked += PositionOnClicked;
        _volume.OnClicked += VolumeOnClicked;
        
        _position.SetVisible(_controller.ConfigurationService.Get().ShowSeek);
        _volume.SetVisible(_controller.ConfigurationService.Get().ShowVolume);

        _initialized = true;
    }

    private void ConfigurationServiceOnSaved(object? sender, EventArgs e)
    {
        _controller.CloseExtension();
        
        if (!_initialized) return;
        
        _position.Active = false;
        _volume.Active = false;
        _position.SetVisible(_controller.ConfigurationService.Get().ShowSeek);
        _volume.SetVisible(_controller.ConfigurationService.Get().ShowVolume);
    }

    private void PlayerServiceOnPlayerVolumeChanged(object? sender, PlayerVolumeArgs e)
    {
        GtkHelper.GtkDispatch(() =>
        {
            if (_controller.PlayerService.IsMuted())
            {
                _volume.SetIconName("audio-volume-muted-symbolic");
            }
            else
            {
                var volume = _controller.PlayerService.GetVolumePercent();
                if (volume > 70)
                {
                    _volume.SetIconName("audio-volume-high-symbolic");
                }
                else if (volume > 30)
                {
                    _volume.SetIconName("audio-volume-medium-symbolic");
                }
                else
                {
                    _volume.SetIconName("audio-volume-low-symbolic");
                }
            }
        });
    }

    private void VolumeOnClicked(Button sender, EventArgs args)
    {
        if (_controller.IsActive(ExtendedType.Volume))
        {
            _volume.Active = false;
            _controller.CloseExtension();
        }
        else if (_controller.IsActive(ExtendedType.Position))
        {
            _volume.Active = true;
            _position.Active = false;
            _controller.ShowExtension(ExtendedType.Volume);
        }
        else
        {
            _volume.Active = true;
            _controller.ShowExtension(ExtendedType.Volume);
        }
    }

    private void PositionOnClicked(Button sender, EventArgs args)
    {
        if (_controller.IsActive(ExtendedType.Position))
        {
            _position.Active = false;
            _controller.CloseExtension();
        }
        else if (_controller.IsActive(ExtendedType.Volume))
        {
            _position.Active = true;
            _volume.Active = false;
            _controller.ShowExtension(ExtendedType.Position);
        }
        else
        {
            _position.Active = true;
            _controller.ShowExtension(ExtendedType.Position);
        }
    }

    public override void Dispose()
    {
        _controller.ConfigurationService.OnSaved -= ConfigurationServiceOnSaved;
        _controller.PlayerService.OnPlayerVolumeChanged -= PlayerServiceOnPlayerVolumeChanged;
        
        _position.OnClicked -= PositionOnClicked;
        _volume.OnClicked -= VolumeOnClicked;
        base.Dispose();
    }
}