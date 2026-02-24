using Adw.Internal;
using JellyTune.Gnome.Helpers;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Events;

namespace JellyTune.Gnome.Views;

public partial class ArtistView : Adw.Dialog
{
    private ArtistController  _controller;

    [Gtk.Connect] private readonly Adw.Spinner _spinner;
    [Gtk.Connect] private readonly Gtk.ScrolledWindow _results;
    [Gtk.Connect] private readonly Adw.StatusPage _noresults;
    [Gtk.Connect] private readonly Adw.StatusPage _noconnection;
    
    [Gtk.Connect] private readonly Gtk.Label _description;
    [Gtk.Connect] private readonly Gtk.Label _artist;

    [Gtk.Connect] private readonly Gtk.Label _country;
    [Gtk.Connect] private readonly Gtk.Label _from;
    [Gtk.Connect] private readonly Gtk.Label _separator;
    [Gtk.Connect] private readonly Gtk.Label _to;
    
    private ArtistView(Gtk.Builder builder) : base(
        new DialogHandle(builder.GetPointer("_root"), false))
    {
        builder.Connect(this);
    }
    
    public ArtistView(ArtistController controller) : this(GtkHelper.BuilderFromFile("artist"))
    {
        _controller = controller;
        _controller.OnArtistChanged += ControllerOnArtistChanged;
        _results.SetVisible(false);
        _noresults.SetVisible(false);
        _noconnection.SetVisible(false);
        _separator.SetVisible(false);
        _spinner.SetVisible(true);
    }

    private void ControllerOnArtistChanged(object? sender, ArtistArgs e)
    {
        if (e.UpdateDetails)
        {
            GtkHelper.GtkDispatch(() =>
            {
                _artist.SetText(_controller.Name ?? string.Empty);
                _country.SetText(_controller.Area ?? string.Empty);

                if (_controller.YearFrom != null)
                {
                    _from.SetText(_controller.YearFrom.ToString());
                    _separator.SetVisible(true);
                }
                else
                {
                    _separator.SetVisible(false);
                }

                if (_controller.YearTo != null)
                {
                    _to.SetText(_controller.YearTo.ToString());
                }
                else
                {
                    _to.SetText("Present");
                }

                _description.SetText(_controller.Description ?? string.Empty);
            });
        }

        if (e.IsLoading)
        {
            // Reset separator state
            _separator.SetVisible(false);

            _results.SetVisible(false);
            _noresults.SetVisible(false);
            _noconnection.SetVisible(false);
            _spinner.SetVisible(true);
        }
        else
        {
            _spinner.SetVisible(false);

            if (e.HasError)
                _noconnection.SetVisible(true);
            else if (string.IsNullOrWhiteSpace(_controller.Description))
                _noresults.SetVisible(true);
            else
                _results.SetVisible(true);
        }
    }

    /*
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
        });
    }
*/
    public override void Dispose()
    {
        _controller.OnArtistChanged += ControllerOnArtistChanged;
        base.Dispose();
    }
}
