using Gdk;
using GLib;
using GObject;
using Gtk;
using JellyTune.Gnome.Helpers;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using Button = Gtk.Button;

namespace JellyTune.Gnome.Views;


[Subclass<Box>(qualifiedName: "JellyTunePlayerView")]
[Template<AssemblyResource>("JellyTune.Gnome.Blueprints.player.ui")]
public partial class PlayerView
{
    private PlayerExtendedController _extendedController;
    private PlayerController _controller;

    private PlayerExtendedButtonView _extendedButtonView;
    
    [Connect] private Box _container;
    [Connect] private Box _actions;
    [Connect] private Image _albumArt;
    [Connect] private Button _skipBackward;
    [Connect] private Button _play;
    [Connect] private Button _skipForward;
    [Connect] private Button _lyrics;
    [Connect] private Button _album;
    [Connect] private Label _track;
    [Connect] private Label _artist;

    private bool _initialized;
    
    public static PlayerView NewWithValues(PlayerController controller, PlayerExtendedController extendedController)
    {
        var obj = NewWithProperties([]);
        obj._controller = controller;
        obj._extendedController = extendedController;
        obj.InitializeController();
        return obj;
    }

    private void InitializeController()
    {
        _extendedButtonView = PlayerExtendedButtonView.NewWithValues(_extendedController);
        _controller.PlayerService.OnPlayerStateChanged += OnPlayerStateChanged;
        _controller.ConfigurationService.OnSaved += ConfigurationServiceOnSaved;
        _actions.Append(_extendedButtonView);
        _skipBackward.OnClicked += SkipBackwardOnClicked;
        _play.OnClicked += PlayerPlayOnClicked;
        _skipForward.OnClicked += SkipForwardOnClicked;
        _lyrics.OnClicked += LyricsOnOnClicked;
        _album.OnClicked += AlbumOnClicked;
        
        var click = GestureClick.New();
        _albumArt.AddController(click);
        click.OnReleased += (_, _) =>
        {
            _controller.ShowPlaylist();
        };

        var key = EventControllerKey.New();
        _albumArt.AddController(key);
        key.OnKeyReleased += (_, _) =>
        {
            _controller.ShowPlaylist();
        };

        _lyrics.SetVisible(_controller.ConfigurationService.Get().ShowLyrics);
        _album.SetVisible(_controller.ConfigurationService.Get().ShowCurrentAlbum);

        _initialized = true;
    }

    private void UpdateTrack()
    {
        if (_controller.Album != null)
            _artist.SetText(_controller.Album.Artist != null ? _controller.Album.Artist : string.Empty);
        
        if (_controller.Artwork != null)
        {
            var bytes = Bytes.New(_controller.Artwork);
            var texture = Texture.NewFromBytes(bytes);
            _albumArt.SetFromPaintable(texture);
        }
        else
        {
            _albumArt.Clear();
        }
        
        if (_controller.SelectedTrack != null)
        {
            _track.SetText(_controller.SelectedTrack.Name != null ? _controller.SelectedTrack.Name : string.Empty);
            _lyrics.SetSensitive(_controller.SelectedTrack.HasLyrics);
            _skipForward.SetSensitive(_controller.PlayerService.HasNextTrack());
            _skipBackward.SetSensitive(_controller.PlayerService.HasPreviousTrack());
        }
    }
    
    private void SkipForwardOnClicked(Button sender, EventArgs args)
    {
        _controller.PlayerService.NextTrackAsync();
    }

    private void SkipBackwardOnClicked(Button sender, EventArgs args)
    {
        _controller.PlayerService.PreviousTrackAsync();
    }
    
    private void PlayerPlayOnClicked(Button sender, EventArgs args)
    {
        _controller.PlayerService.StartOrPauseTrackAsync();
    }
    
    private void AlbumOnClicked(Button sender, EventArgs args)
    {
        if (GetRoot() is Window win)
        {
            var albumId = _controller.PlayerService.GetSelectedAlbum() != null ? _controller.PlayerService.GetSelectedAlbum()?.Id : null;
            if (!albumId.HasValue) return;
            
            win.ActivateAction("win.open_album", Variant.NewString(albumId.ToString() ?? string.Empty));
        }
    }

    private void ConfigurationServiceOnSaved(object? sender, EventArgs e)
    {
        if (!_initialized) return;
        
        _lyrics.SetVisible(_controller.ConfigurationService.Get().ShowLyrics);
        _album.SetVisible(_controller.ConfigurationService.Get().ShowCurrentAlbum);
    }

    private void LyricsOnOnClicked(Button sender, EventArgs args)
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