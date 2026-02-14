using Gtk.Internal;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using JellyTune.Gnome.Helpers;

namespace JellyTune.Gnome.Views;

public class PlayerView : Gtk.Box
{
    private readonly PlayerExtendedController _extendedController;
    private readonly PlayerController _controller;

    private readonly PlayerExtendedButtonView _extendedButtonView;

    private bool _isDisposed;
    
    [Gtk.Connect] private readonly Gtk.Box _container;
    [Gtk.Connect] private readonly Gtk.Box _actions;
    [Gtk.Connect] private readonly Gtk.Image _albumArt;
    [Gtk.Connect] private readonly Gtk.Button _skipBackward;
    [Gtk.Connect] private readonly Gtk.Button _play;
    [Gtk.Connect] private readonly Gtk.Button _skipForward;
    [Gtk.Connect] private readonly Gtk.Button _lyrics;
    [Gtk.Connect] private readonly Gtk.Label _track;
    [Gtk.Connect] private readonly Gtk.Label _artist;

    private PlayerView(Gtk.Builder builder) : base(
        new BoxHandle(builder.GetPointer("_root"), false))
    {
        builder.Connect(this);
    }

    private void UpdateTrack()
    {
        _artist.SetText(GLib.Markup.EscapeText(_controller.Album.Artist));
        if (_controller.Artwork != null)
        {
            var bytes = GLib.Bytes.New(_controller.Artwork);
            var texture = Gdk.Texture.NewFromBytes(bytes);
            _albumArt.SetFromPaintable(texture);
        }
        else
        {
            _albumArt.Clear();
        }
        
        if (_controller.SelectedTrack != null)
        {
            _track.SetText(_controller.SelectedTrack.Name);
            _lyrics.SetSensitive(_controller.SelectedTrack.HasLyrics);
            _skipForward.SetSensitive(_controller.PlayerService.HasNextTrack());
            _skipBackward.SetSensitive(_controller.PlayerService.HasPreviousTrack());
        }
    }
    
    private void SkipForwardOnClicked(Gtk.Button sender, EventArgs args)
    {
        _controller.PlayerService.NextTrackAsync();
    }

    private void SkipBackwardOnClicked(Gtk.Button sender, EventArgs args)
    {
        _controller.PlayerService.PreviousTrackAsync();
    }
    
    private void PlayerPlayOnClicked(Gtk.Button sender, EventArgs args)
    {
        _controller.PlayerService.StartOrPauseTrackAsync();
    }

    public PlayerView(PlayerController controller, PlayerExtendedController extendedController) : this(GtkHelper.BuilderFromFile("player"))
    {
        _controller = controller;
        _extendedController = extendedController;
        _extendedButtonView = new PlayerExtendedButtonView(_extendedController);
        _actions.Append(_extendedButtonView);
        _skipBackward.OnClicked += SkipBackwardOnClicked;
        _play.OnClicked += PlayerPlayOnClicked;
        _skipForward.OnClicked += SkipForwardOnClicked;
        _lyrics.OnClicked += LyricsOnOnClicked;
        _controller.PlayerService.OnPlayerStateChanged += OnPlayerStateChanged;

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

    private void LyricsOnOnClicked(Gtk.Button sender, EventArgs args)
    {
        _controller.ShowShowLyrics();
    }

    private void OnPlayerStateChanged(object? sender, PlayerStateArgs e)
    {
        var state = e.State;

        GtkHelper.GtkDispatch(() =>
        {
            if (_isDisposed) return;
            if (!IsVisible()) return;

            switch (state)
            {
                case PlayerState.Stopped:
                case PlayerState.Paused:
                    _play.IconName = "media-playback-start-symbolic";
                    _play.TooltipText = "Play track";
                    UpdateTrack();
                    break;

                case PlayerState.Playing:
                    _play.IconName = "media-playback-pause-symbolic";
                    _play.TooltipText = "Pause track";
                    UpdateTrack();
                    break;

                case PlayerState.SkipNext:
                case PlayerState.SkipPrevious:
                    UpdateTrack();
                    break;
            }
        });
    }

    public override void Dispose()
    {
        _isDisposed = true;
        _controller.PlayerService.OnPlayerStateChanged -= OnPlayerStateChanged;
        base.Dispose();
    }
}