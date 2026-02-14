using Gtk.Internal;
using JellyTune.Gnome.Helpers;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using Button = Gtk.Button;

namespace JellyTune.Gnome.Views;

public partial class PlayerExtendedButtonView : Gtk.Box
{
    private readonly PlayerExtendedController _controller;

    [Gtk.Connect] private readonly Gtk.ToggleButton _position;
    [Gtk.Connect] private readonly Gtk.ToggleButton _volume;

    private PlayerExtendedButtonView(Gtk.Builder builder) : base(
        new BoxHandle(builder.GetPointer("_root"), false))
    {
        builder.Connect(this);
    }

    public PlayerExtendedButtonView(PlayerExtendedController controller) : this(
        GtkHelper.BuilderFromFile("player_extended_button"))
    {
        _controller = controller;
        _position.OnClicked += PositionOnClicked;
        _volume.OnClicked += VolumeOnClicked;
        
        _controller.PlayerService.OnPlayerVolumeChanged += PlayerServiceOnPlayerVolumeChanged;
        _controller.ConfigurationService.OnSaved += ConfigurationServiceOnSaved;
        
        SetVisible(_controller.ConfigurationService.Get().ShowExtendedControls);
    }

    private void ConfigurationServiceOnSaved(object? sender, EventArgs e)
    {
        _controller.CloseExtension();
        _position.Active = false;
        _volume.Active = false;
        SetVisible(_controller.ConfigurationService.Get().ShowExtendedControls);
    }

    private void PlayerServiceOnPlayerVolumeChanged(object? sender, PlayerVolumeArgs e)
    {
        if (e.IsMuted)
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