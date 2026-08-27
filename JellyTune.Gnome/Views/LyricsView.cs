using JellyTune.Shared.Controls;
using JellyTune.Gnome.Helpers;

namespace JellyTune.Gnome.Views;

[GObject.Subclass<Adw.Dialog>(qualifiedName: "JellyTuneLyricsView")]
[Gtk.Template<Gtk.AssemblyResource>("JellyTune.Gnome.Blueprints.lyrics.ui")]
public partial class LyricsView
{
    private LyricsController  _controller;

    [Gtk.Connect] private Adw.Spinner _spinner;
    [Gtk.Connect] private Gtk.Revealer _results;
    
    [Gtk.Connect] private Gtk.Label _lyrics;
    [Gtk.Connect] private Gtk.Image _albumArt;
    [Gtk.Connect] private Gtk.Label _track;
    [Gtk.Connect] private Gtk.Label _artist;

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
            _track.SetLabel(_controller.TrackName);
            _artist.SetLabel(_controller.ArtistName);
        
            if (!string.IsNullOrWhiteSpace(_controller.Lyrics))
                _lyrics.SetLabel(_controller.Lyrics);;

            if (_controller.AlbumArt != null)
            {
                var bytes = GLib.Bytes.New(_controller.AlbumArt);
                var texture = Gdk.Texture.NewFromBytes(bytes);
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
