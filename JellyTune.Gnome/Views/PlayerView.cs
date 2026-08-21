using Gtk.Internal;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using JellyTune.Gnome.Helpers;
using Button = Gtk.Button;

namespace JellyTune.Gnome.Views;


[GObject.Subclass<Gtk.Box>(qualifiedName: "JellyTunePlayerView")]
[Gtk.Template<Gtk.AssemblyResource>("JellyTune.Gnome.Blueprints.player.ui")]
public partial class PlayerView
{
    private readonly PlayerExtendedController _extendedController;
    private readonly PlayerController _controller;

    private readonly PlayerExtendedButtonView _extendedButtonView;
    
    [Gtk.Connect] private Gtk.Box _container;
    [Gtk.Connect] private Gtk.Box _actions;
    [Gtk.Connect] private Gtk.Image _albumArt;
    [Gtk.Connect] private Gtk.Button _skipBackward;
    [Gtk.Connect] private Gtk.Button _play;
    [Gtk.Connect] private Gtk.Button _skipForward;
    [Gtk.Connect] private Gtk.Button _lyrics;
    [Gtk.Connect] private Gtk.Button _album;
    [Gtk.Connect] private Gtk.Label _track;
    [Gtk.Connect] private Gtk.Label _artist;

    private bool _initialized = false;
    
    private void UpdateTrack()
    {
        if (_controller.Album != null)
            _artist.SetText(_controller.Album.Artist);
        
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

    public PlayerView(PlayerController controller, PlayerExtendedController extendedController)
    {
        _controller = controller;
        _extendedController = extendedController;
        _extendedButtonView = new PlayerExtendedButtonView(_extendedController);
        _controller.PlayerService.OnPlayerStateChanged += OnPlayerStateChanged;
        _controller.ConfigurationService.OnSaved += ConfigurationServiceOnSaved;
    }

    partial void Initialize()
    {
        _actions.Append(_extendedButtonView);
        _skipBackward.OnClicked += SkipBackwardOnClicked;
        _play.OnClicked += PlayerPlayOnClicked;
        _skipForward.OnClicked += SkipForwardOnClicked;
        _lyrics.OnClicked += LyricsOnOnClicked;
        _album.OnClicked += AlbumOnClicked;
        
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

        _lyrics.SetVisible(_controller.ConfigurationService.Get().ShowLyrics);
        _album.SetVisible(_controller.ConfigurationService.Get().ShowCurrentAlbum);

        _initialized = true;
    }
    
    
    private void AlbumOnClicked(Button sender, EventArgs args)
    {
        if (GetRoot() is Gtk.Window win)
        {
            var albumId = _controller.PlayerService.GetSelectedAlbum()?.Id;
            if (!albumId.HasValue) return;
            
            win.ActivateAction("win.open_album", GLib.Variant.NewString(albumId.ToString()));
        }
    }

    private void ConfigurationServiceOnSaved(object? sender, EventArgs e)
    {
        if (!_initialized) return;
        
        _lyrics.SetVisible(_controller.ConfigurationService.Get().ShowLyrics);
        _album.SetVisible(_controller.ConfigurationService.Get().ShowCurrentAlbum);
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
                
                case PlayerState.LoadedInfo:
                case PlayerState.LoadedArtwork:
                case PlayerState.SkipNext:
                case PlayerState.SkipPrevious:
                    UpdateTrack();
                    break;
            }
        });
    }

    public override void Dispose()
    {
        _controller.ConfigurationService.OnSaved -= ConfigurationServiceOnSaved;
        _controller.PlayerService.OnPlayerStateChanged -= OnPlayerStateChanged;
        base.Dispose();
    }
}