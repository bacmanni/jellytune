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
    private readonly IConfigurationService  _configurationService;
    
    [Gtk.Connect] private readonly Gtk.Label _currentPosition;
    [Gtk.Connect] private readonly Gtk.Scale _durationScale;
    [Gtk.Connect] private readonly Gtk.Label _totalLength;
    
    private Guid? _playingTrackId;
    
    private PlayerPositionView(Gtk.Builder builder) : base(
        new BoxHandle(builder.GetPointer("_root"), false))
    {
        builder.Connect(this);
    }

    public PlayerPositionView(IPlayerService playerService, IConfigurationService configurationService) : this(
        Blueprint.BuilderFromFile("player_position"))
    {
        _playerService = playerService;
        _configurationService = configurationService;
        _playerService.OnPlayerStateChanged += PlayerServiceOnPlayerStateChanged;
        _playerService.OnPlayerPositionChanged += PlayerServiceOnPlayerPositionChanged;
        _durationScale.OnChangeValue += DurationScaleOnChangeValue;
        _configurationService.OnSaved += ConfigurationServiceOnOnSaved;
        SetVisible(_configurationService.Get().ShowPlayerDuration);
        _currentPosition.SetVisible(_configurationService.Get().ShowPlayerDurationLabel);
        _totalLength.SetVisible(_configurationService.Get().ShowPlayerDurationLabel);
    }

    private void ConfigurationServiceOnOnSaved(object? sender, EventArgs e)
    {
        SetVisible(_configurationService.Get().ShowPlayerDuration);
        _currentPosition.SetVisible(_configurationService.Get().ShowPlayerDurationLabel);
        _totalLength.SetVisible(_configurationService.Get().ShowPlayerDurationLabel);
    }

    private void PlayerServiceOnPlayerPositionChanged(object? sender, PlayerPositionArgs e)
    {
        var time = TimeSpan.FromSeconds(e.Position);
        _currentPosition.SetText($"{(int)time.TotalMinutes}:{time.Seconds:00}");
        _durationScale.Adjustment.Value = e.Position;
    }

    private bool DurationScaleOnChangeValue(Range sender, Range.ChangeValueSignalArgs args)
    {
        _playerService.SeekTrack(args.Value);
        return true;
    }

    private void PlayerServiceOnPlayerStateChanged(object? sender, PlayerStateArgs e)
    {
        if (!Visible) return;
        
        if (e.State is not PlayerState.Playing) return;
        
        if (e.SelectedTrack?.Id == _playingTrackId) return;
        
        _durationScale.Adjustment.Lower = 0;
        _durationScale.Adjustment.Value = 0;
        _durationScale.Adjustment.Upper = e.SelectedTrack.RunTime.TotalSeconds;
        _totalLength.SetText($"{(int)e.SelectedTrack.RunTime.TotalMinutes}:{e.SelectedTrack.RunTime.Seconds:00}");
        _currentPosition.SetText("0:00");
        _playingTrackId =  e.SelectedTrackId;
    }

    public override void Dispose()
    {
        _durationScale.OnChangeValue -= DurationScaleOnChangeValue;
        _playerService.OnPlayerStateChanged -= PlayerServiceOnPlayerStateChanged;
        base.Dispose();
    }
}
