using Gtk.Internal;
using JellyTune.Gnome.Helpers;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using Button = Gtk.Button;
using Range = Gtk.Range;

namespace JellyTune.Gnome.Views;

public partial class PlayerExtendedView : Gtk.Revealer
{
    private readonly PlayerExtendedController _controller;

    [Gtk.Connect] private readonly Gtk.Stack _extendedStack;
    [Gtk.Connect] private readonly Gtk.Box _position;
    [Gtk.Connect] private readonly Gtk.Box _volume;
    
    // Volume
    [Gtk.Connect] private readonly Gtk.Button _muteButton;
    [Gtk.Connect] private readonly Gtk.Scale _volumeScale;
    [Gtk.Connect] private readonly Gtk.Button _volumeButton;
    
    // Duration 
    [Gtk.Connect] private readonly Gtk.Label _currentPosition;
    [Gtk.Connect] private readonly Gtk.Scale _durationScale;
    [Gtk.Connect] private readonly Gtk.Label _totalLength;
    
    private Guid? _playingTrackId;
    
    private PlayerExtendedView(Gtk.Builder builder) : base(
        new RevealerHandle(builder.GetPointer("_root"), false))
    {
        builder.Connect(this);
    }

    public PlayerExtendedView(PlayerExtendedController controller) : this(
        GtkHelper.BuilderFromFile("player_extended"))
    {
        _controller = controller;
        _controller.OnShowHide += ControllerOnShowHide;
        _controller.PlayerService.OnPlayerStateChanged += PlayerServiceOnPlayerStateChanged;
        _controller.PlayerService.OnPlayerPositionChanged += PlayerServiceOnPlayerPositionChanged;
        _controller.PlayerService.OnPlayerVolumeChanged += PlayerServiceOnPlayerVolumeChanged;
        
        // Volume
        _muteButton.OnClicked += MuteButtonOnClicked;
        _volumeScale.OnChangeValue += VolumeScaleOnChangeValue;
        _volumeButton.OnClicked += VolumeButtonOnClicked;
        
        // Duration
        _durationScale.OnChangeValue += DurationScaleOnChangeValue;
    }

    private void VolumeButtonOnClicked(Button sender, EventArgs args)
    {
        _controller.PlayerService.SetVolumePercent(100);
    }

    private void PlayerServiceOnPlayerVolumeChanged(object? sender, PlayerVolumeArgs e)
    {
        if (_volumeScale.Adjustment == null) return;

        GtkHelper.GtkDispatch(() =>
        {
            _volumeScale.Adjustment.Value = _controller.PlayerService.GetVolumePercent();
        });
    }

    private void MuteButtonOnClicked(Button sender, EventArgs args)
    {
        var currentValue = _controller.PlayerService.IsMuted();
        _controller.PlayerService.SetMuted(!currentValue);
    }

    private bool VolumeScaleOnChangeValue(Range sender, Range.ChangeValueSignalArgs args)
    {
        _controller.PlayerService.SetVolumePercent(args.Value);
        return true;
    }

    private void ControllerOnShowHide(object? sender, ExtendedShow e)
    {
        RevealChild = e.IsVisible;

        if (e.Type == ExtendedType.Position)
        {
            _extendedStack.VisibleChild = _position;
        }
        else if (e.Type == ExtendedType.Volume)
        {
            _extendedStack.VisibleChild = _volume;
        }
    }

    private void PlayerServiceOnPlayerPositionChanged(object? sender, PlayerPositionArgs e)
    {
        var time = TimeSpan.FromSeconds(e.Position);
        _currentPosition.SetText($"{(int)time.TotalMinutes}:{time.Seconds:00}");
        _durationScale.Adjustment.Value = e.Position;
    }

    private bool DurationScaleOnChangeValue(Range sender, Range.ChangeValueSignalArgs args)
    {
        _controller.PlayerService.SeekTrack(args.Value);
        return true;
    }

    private void PlayerServiceOnPlayerStateChanged(object? sender, PlayerStateArgs e)
    {
        if (!Visible) return;
        if (e.State is not PlayerState.Playing) return;
        
        var selectedTrack = e.SelectedTrack;
        
        if (selectedTrack?.Id == _playingTrackId) return;
        
        var volume = _controller.PlayerService.GetVolumePercent();
        var runtime = selectedTrack.RunTime;
        
        GtkHelper.GtkDispatch(() =>
        {
            _volumeScale.Adjustment.Value = volume;
            _durationScale.Adjustment.Lower = 0;
            _durationScale.Adjustment.Value = 0;
            _durationScale.Adjustment.Upper = runtime.TotalSeconds;
            _currentPosition.SetText("0:00");
            _totalLength.SetText($"{(int)runtime.TotalMinutes}:{runtime.Seconds:00}");
            _playingTrackId = selectedTrack.Id;
        });
    }

    public override void Dispose()
    {
        _controller.OnShowHide -= ControllerOnShowHide;
        
        _durationScale.OnChangeValue -= DurationScaleOnChangeValue;
        
        _volumeScale.OnChangeValue -= VolumeScaleOnChangeValue;
        _muteButton.OnClicked -= MuteButtonOnClicked;
        _volumeButton.OnClicked -= VolumeButtonOnClicked;
        
        _controller.PlayerService.OnPlayerStateChanged -= PlayerServiceOnPlayerStateChanged;
        _controller.PlayerService.OnPlayerPositionChanged -= PlayerServiceOnPlayerPositionChanged;
        _controller.PlayerService.OnPlayerVolumeChanged -= PlayerServiceOnPlayerVolumeChanged;
        base.Dispose();
    }
}
