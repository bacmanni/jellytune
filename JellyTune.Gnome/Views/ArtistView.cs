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
    
    [Gtk.Connect] private readonly Gtk.Label _description;
    [Gtk.Connect] private readonly Gtk.Label _artist;

    [Gtk.Connect] private readonly Gtk.Label _country;
    [Gtk.Connect] private readonly Gtk.Label _duration;
    
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
                    _duration.SetText(_controller.YearTo != null
                        ? $"{_controller.YearFrom} - {_controller.YearTo}"
                        : $"{_controller.YearFrom} - Present");
                }
  
                _description.SetText(_controller.Description ?? string.Empty);
            });
        }

        if (e.IsLoading)
        {
            _results.SetVisible(false);
            _noresults.SetVisible(false);
            _spinner.SetVisible(true);
        }
        else
        {
            _spinner.SetVisible(false);
            
            if (string.IsNullOrWhiteSpace(_controller.Description))
                _noresults.SetVisible(true);
            else
                _results.SetVisible(true);
        }
    }

    public override void Dispose()
    {
        _controller.OnArtistChanged += ControllerOnArtistChanged;
        base.Dispose();
    }
}
