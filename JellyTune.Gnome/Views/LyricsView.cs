using Gdk;
using GLib;
using GObject;
using Gtk;
using JellyTune.Gnome.Helpers;
using JellyTune.Shared.Controls;
using Dialog = Adw.Dialog;
using Spinner = Adw.Spinner;

namespace JellyTune.Gnome.Views;

[Subclass<Dialog>(qualifiedName: "JellyTuneLyricsView")]
[Template<AssemblyResource>("JellyTune.Gnome.Blueprints.lyrics.ui")]
public partial class LyricsView
{
    private LyricsController  _controller;

    [Connect] private Spinner _spinner;
    [Connect] private Revealer _results;
    
    [Connect] private Label _lyrics;
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
        _controller.OnLyricsUpdated += ControllerOnOnLyricsUpdated;
        _results.SetVisible(false);
        _spinner.SetVisible(true);
    }

    private void ControllerOnOnLyricsUpdated(object? sender, EventArgs e)
    {
        GtkHelper.GtkDispatch(() =>
        {
            _track.SetLabel(_controller.TrackName ?? string.Empty);
            _artist.SetLabel(_controller.ArtistName ?? string.Empty);
        
            if (!string.IsNullOrWhiteSpace(_controller.Lyrics))
                _lyrics.SetLabel(_controller.Lyrics);

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
        _controller.OnLyricsUpdated -= ControllerOnOnLyricsUpdated;
        base.Dispose();
    }
}
