using GObject;
using Gtk;
using JellyTune.Gnome.Helpers;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using Button = Gtk.Button;
using Range = Gtk.Range;

namespace JellyTune.Gnome.Views;

[Subclass<Revealer>(qualifiedName: "JellyTunePlayerExtendedView")]
[Template<AssemblyResource>("JellyTune.Gnome.Blueprints.player_extended.ui")]
public partial class PlayerExtendedView
{
    private PlayerExtendedController _controller;

    [Connect] private Stack _extendedStack;
    [Connect] private Box _position;
    [Connect] private Box _volume;
    
    // Volume
    [Connect] private Button _muteButton;
    [Connect] private Scale _volumeScale;
    [Connect] private Button _volumeButton;
    
    // Duration 
    [Connect] private Label _currentPosition;
    [Connect] private Scale _durationScale;
    [Connect] private Label _totalLength;
    
    private Guid? _playingTrackId;

    public static PlayerExtendedView NewWithValues(PlayerExtendedController controller)
    {
        var obj = NewWithProperties([]);
        obj._controller = controller;
        obj.InitializeController();
        return obj;
    }

    private void InitializeController()
    {
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
        var position = e.Position;

        GtkHelper.GtkDispatch(() =>
        {
            _currentPosition.SetText($"{(int)time.TotalMinutes}:{time.Seconds:00}");
            
            if (_durationScale.Adjustment != null)
                _durationScale.Adjustment.Value = position;
        });
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
        
        if (selectedTrack != null ? selectedTrack.Id == _playingTrackId : false) return;
        
        var volume = _controller.PlayerService.GetVolumePercent();
        var runtime = selectedTrack != null ? selectedTrack.RunTime : TimeSpan.MinValue;
        
        GtkHelper.GtkDispatch(() =>
        {
            if (_volumeScale.Adjustment != null)
                _volumeScale.Adjustment.Value = volume;

            if (_durationScale.Adjustment != null)
            {
                _durationScale.Adjustment.Lower = 0;
                _durationScale.Adjustment.Value = 0;
                _durationScale.Adjustment.Upper = runtime.TotalSeconds;
            }
            
            _currentPosition.SetText("0:00");
            _totalLength.SetText($"{(int)runtime.TotalMinutes}:{runtime.Seconds:00}");
            
            if (selectedTrack != null)
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
