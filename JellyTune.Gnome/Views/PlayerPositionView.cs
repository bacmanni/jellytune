using System.Timers;
using Adw.Internal;
using Gtk.Internal;
using JellyTune.Gnome.Helpers;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using JellyTune.Shared.Services;
using Range = Gtk.Range;
using Timer = System.Timers.Timer;

namespace JellyTune.Gnome.Views;

public partial class PlayerPositionView : Gtk.Box
{
    private readonly IPlayerService _playerService;

    [Gtk.Connect] private readonly Gtk.Scale _durationScale;
    
    private Guid? _playingTrackId;
    
    private PlayerPositionView(Gtk.Builder builder) : base(
        new BoxHandle(builder.GetPointer("_root"), false))
    {
        builder.Connect(this);
    }

    public PlayerPositionView(IPlayerService playerService) : this(
        Blueprint.BuilderFromFile("player_position"))
    {
        _playerService = playerService;
        _playerService.OnPlayerStateChanged += PlayerServiceOnPlayerStateChanged;
        _playerService.OnPlayerPositionChanged += PlayerServiceOnPlayerPositionChanged;
        _durationScale.OnChangeValue += DurationScaleOnChangeValue;
    }

    private void PlayerServiceOnPlayerPositionChanged(object? sender, PlayerPositionArgs e)
    {
        _durationScale.Adjustment.Value = e.Position;
    }

    private bool DurationScaleOnChangeValue(Range sender, Range.ChangeValueSignalArgs args)
    {
        _playerService.SeekTrack(args.Value);
        return true;
    }

    private void PlayerServiceOnPlayerStateChanged(object? sender, PlayerStateArgs e)
    {
        if (e.State is not PlayerState.Playing) return;
        
        if (e.SelectedTrack?.Id == _playingTrackId) return;
        
        _durationScale.Adjustment.Lower = 0;
        _durationScale.Adjustment.Value = 0;
        _durationScale.Adjustment.Upper = e.SelectedTrack.RunTime.TotalSeconds;
        _playingTrackId =  e.SelectedTrackId;
    }

    public override void Dispose()
    {
        _durationScale.OnChangeValue -= DurationScaleOnChangeValue;
        _playerService.OnPlayerStateChanged -= PlayerServiceOnPlayerStateChanged;
        base.Dispose();
    }
}
