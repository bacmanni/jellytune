using Adw;
using Gdk;
using GLib;
using GObject;
using Gtk;
using JellyTune.Gnome.Helpers;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Events;
using JellyTune.Shared.Models;
using Dialog = Adw.Dialog;
using Spinner = Adw.Spinner;
using TimeSpan = System.TimeSpan;

namespace JellyTune.Gnome.Views;

[Subclass<Dialog>(qualifiedName: "JellyTuneLyricsView")]
[Template<AssemblyResource>("JellyTune.Gnome.Blueprints.lyrics.ui")]
public partial class LyricsView
{
    private LyricsController  _controller;
    private LyricItem? _previousItem;
    
    [Connect] private ToastOverlay _toastOverlay;
    [Connect] private Spinner _spinner;
    [Connect] private Revealer _results;

    [Connect] private Box _lyricsBox;
    [Connect] private Image _albumArt;
    [Connect] private Label _track;
    [Connect] private Label _artist;

    public static LyricsView NewWithValues(LyricsController controller)
    {
        var obj = NewWithProperties([]);
        obj._controller = controller;
        obj.InitializeController();
        return obj;
    }

    private void InitializeController()
    {
        _controller.PlayerService.OnPlayerPositionChanged += PlayerServiceOnPlayerPositionChanged;
        _controller.OnLyricsUpdated += ControllerOnOnLyricsUpdated;
        _results.SetVisible(false);
        _spinner.SetVisible(true);
        
        var clickGesture = GestureClick.New();

        clickGesture.OnPressed += (sender, args) =>
        {
            //if (string.IsNullOrWhiteSpace(_controller.Lyrics)) return;
            
            var display = _lyricsBox.GetDisplay();
            var clipboard = display.GetClipboard();
            //clipboard.SetText(_controller.Lyrics);
            
            var toast = Toast.New("Copied to clipboard");
            toast.SetTimeout(2);
            _toastOverlay.AddToast(toast);
        };

        _lyricsBox.AddController(clickGesture);
    }

    private void PlayerServiceOnPlayerPositionChanged(object? sender, PlayerPositionArgs e)
    {
        if(!_controller.ConfigurationService.Get().ShowLyricHighlight) return;
        
        if (_controller.Lyrics.Any())
        {
            var position = TimeSpan.FromSeconds(e.Position);
            var currentLyricItem = _controller.Lyrics.Where(t => t.StartTime <= position).MaxBy(t => t.StartTime);
            if (currentLyricItem is not { StartTime: not null }) return;

            if (_previousItem == null || !_previousItem.Equals(currentLyricItem))
            {
                _previousItem = currentLyricItem;
                
                GtkHelper.GtkDispatch(() =>
                {
                    var child = _lyricsBox.GetFirstChild();
                    while (child != null)
                    {
                        if (child is Label label && !string.IsNullOrWhiteSpace(currentLyricItem.Text) && label.GetText() == currentLyricItem.Text)
                            child.AddCssClass("lyrics-current");
                        else
                            child.RemoveCssClass("lyrics-current");
                    
                        child = child.GetNextSibling();
                    }
                });
            }
        }
    }

    private void ControllerOnOnLyricsUpdated(object? sender, EventArgs e)
    {
        GtkHelper.GtkDispatch(() =>
        {
            _track.SetLabel(_controller.TrackName ?? string.Empty);
            _artist.SetLabel(_controller.ArtistName ?? string.Empty);

            if (_controller.Lyrics.Count > 0)
            {
                while (_lyricsBox.GetFirstChild() is { } child)
                {
                    _lyricsBox.Remove(child);
                }
                
                foreach (var lyric in _controller.Lyrics)
                {
                    var row = Label.New(lyric.Text);
                    row.SetHalign(Align.Start);
                    row.SetWrap(true);
                    _lyricsBox.Append(row);
                }
            }

            if (_controller.AlbumArt != null)
            {
                var bytes = Bytes.New(_controller.AlbumArt);
                var texture = Texture.NewFromBytes(bytes);
                _albumArt.SetFromPaintable(texture);
            }
            
            _spinner.SetVisible(false);
            _results.SetVisible(true);
            _results.SetRevealChild(true);
        });
    }

    public override void Dispose()
    {
        _controller.PlayerService.OnPlayerPositionChanged -= PlayerServiceOnPlayerPositionChanged;
        _controller.OnLyricsUpdated -= ControllerOnOnLyricsUpdated;
        base.Dispose();
    }
}
