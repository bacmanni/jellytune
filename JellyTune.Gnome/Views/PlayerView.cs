using Gtk;
using Gtk.Internal;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using JellyTune.Gnome.Helpers;
using Range = Gtk.Range;

namespace JellyTune.Gnome.Views;

public class PlayerView : Gtk.CenterBox
{
    private readonly Gtk.Widget  _mainWindow;
    private readonly PlayerController _controller;
    private Guid? _playingTrackId;
    
    [Gtk.Connect] private readonly Gtk.Scale _durationScale;
        
    [Gtk.Connect] private readonly Gtk.Box _container;
    [Gtk.Connect] private readonly Gtk.Image _albumArt;
    [Gtk.Connect] private readonly Gtk.Button _skipBackward;
    [Gtk.Connect] private readonly Gtk.Button _play;
    [Gtk.Connect] private readonly Gtk.Button _skipForward;
    [Gtk.Connect] private readonly Gtk.Button _lyrics;
    [Gtk.Connect] private readonly Gtk.Label _track;
    [Gtk.Connect] private readonly Gtk.Label _artist;

    private PlayerView(Gtk.Builder builder) : base(
        new CenterBoxHandle(builder.GetPointer("_root"), false))
    {
        builder.Connect(this);
    }

    private void UpdateTrack(bool begin = false)
    {
        if (_controller.SelectedTrack != null)
        {
            if (begin)
            {
                _durationScale.Adjustment.Lower = 0;
                _durationScale.Adjustment.Value = 0;
                _durationScale.Adjustment.Upper = _controller.SelectedTrack.RunTime.Value.TotalSeconds;
            }
            
            _track.SetText(_controller.SelectedTrack.Name);
            _lyrics.SetSensitive(_controller.SelectedTrack.HasLyrics);
            _skipForward.SetSensitive(_controller.GetPlayerService().HasNextTrack());
            _skipBackward.SetSensitive(_controller.GetPlayerService().HasPreviousTrack());
        }
    }

    private void UpdateAlbum()
    {
        _artist.SetText(GLib.Markup.EscapeText(_controller.Album.Artist));
        _albumArt.Clear();
        UpdateTrack();
    }
    
    private void UpdateArtwork()
    {
        if (_controller.Artwork != null)
        {
            using var bytes = GLib.Bytes.New(_controller.Artwork);
            using var texture = Gdk.Texture.NewFromBytes(bytes);
            _albumArt.SetFromPaintable(texture);
        }
    }
    
    private void SkipForwardOnClicked(Gtk.Button sender, EventArgs args)
    {
        _controller.GetPlayerService().NextTrackAsync();
    }

    private void SkipBackwardOnClicked(Gtk.Button sender, EventArgs args)
    {
        _controller.GetPlayerService().PreviousTrackAsync();
    }
    
    private void PlayerPlayOnClicked(Gtk.Button sender, EventArgs args)
    {
        _controller.GetPlayerService().StartOrPauseTrackAsync();
    }

    public PlayerView(Gtk.Widget mainWindow, PlayerController controller) : this(Blueprint.BuilderFromFile("player"))
    {
        _mainWindow  = mainWindow;
        _controller = controller;
        _skipBackward.OnClicked += SkipBackwardOnClicked;
        _play.OnClicked += PlayerPlayOnClicked;
        _skipForward.OnClicked += SkipForwardOnClicked;
        _lyrics.OnClicked += LyricsOnOnClicked;
        _controller.GetPlayerService().OnPlayerStateChanged += OnPlayerStateChanged;
        _controller.GetPlayerService().OnPlayerPositionChanged += OnPlayerPositionChanged;
        
        _durationScale.OnChangeValue += DurationScaleOnChangeValue;
        
        var click = Gtk.GestureClick.New();
        _albumArt.AddController(click);
        click.OnReleased += (sender, args) =>
        {
            _controller.ShowPlaylist();
        };

        var key = Gtk.EventControllerKey.New();
        _albumArt.AddController(key);
        key.OnKeyReleased += (sender, args) =>
        {
            _controller.ShowPlaylist();
        };
    }

    private bool DurationScaleOnChangeValue(Range sender, Range.ChangeValueSignalArgs args)
    {
        _controller.SeekTrack(args.Value);
        sender.SetValue(args.Value);
        return true;
    }

    private void OnPlayerPositionChanged(object? sender, PlayerPositionArgs e)
    {
        _durationScale.Adjustment.Value = e.Position;
    }

    private void LyricsOnOnClicked(Gtk.Button sender, EventArgs args)
    {
        _controller.ShowShowLyrics();
    }

    private void OnPlayerStateChanged(object? sender, PlayerStateArgs e)
    {
        switch (e.State)
        {
            case PlayerState.Stopped or PlayerState.Paused:
                _play.IconName = "media-playback-start-symbolic";
                UpdateTrack();
                break;
            case PlayerState.Playing:
                _play.IconName = "media-playback-pause-symbolic";
                UpdateTrack(e.SelectedTrackId != _playingTrackId);
                _playingTrackId = e.SelectedTrackId;
                break;
            case PlayerState.SkipNext or PlayerState.SkipPrevious:
                UpdateTrack();
                break;
            case PlayerState.LoadedInfo:
                UpdateAlbum();
                break;
            case PlayerState.LoadedArtwork:
                UpdateArtwork();
                break;
        }
    }

    public override void Dispose()
    {
        _controller.GetPlayerService().OnPlayerStateChanged -= OnPlayerStateChanged;
        _controller.GetPlayerService().OnPlayerPositionChanged -= OnPlayerPositionChanged;
        base.Dispose();
    }
}