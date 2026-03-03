using Adw.Internal;
using JellyTune.Gnome.Helpers;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Events;

namespace JellyTune.Gnome.Views;

public partial class InformationView : Adw.Dialog
{
    private InformationController  _controller;

    [Gtk.Connect] private readonly Adw.Spinner _spinner;
    [Gtk.Connect] private readonly Gtk.ScrolledWindow _results;
    [Gtk.Connect] private readonly Adw.StatusPage _noresults;
    
    [Gtk.Connect] private readonly Gtk.Label _description;
    [Gtk.Connect] private readonly Gtk.Label _title;
    [Gtk.Connect] private readonly Gtk.Box _subtitle;
    
    private InformationView(Gtk.Builder builder) : base(
        new DialogHandle(builder.GetPointer("_root"), false))
    {
        builder.Connect(this);
    }
    
    public InformationView(InformationController controller) : this(GtkHelper.BuilderFromFile("information"))
    {
        _controller = controller;
        _controller.OnInformationChanged += ControllerOnInformationChanged;
        _results.SetVisible(false);
        _noresults.SetVisible(false);
        _spinner.SetVisible(true);
    }

    private void ControllerOnInformationChanged(object? sender, InformationArgs e)
    {
        if (e.UpdateDetails)
        {
            GtkHelper.GtkDispatch(() =>
            {
                _title.SetText(_controller.Title ?? string.Empty);
                
                var child = _subtitle.GetFirstChild();
                while (child != null)
                {
                    var next = child.GetNextSibling();
                    _subtitle.Remove(child);
                    child = next;
                }
                
                foreach (var subtitle in _controller.Subtitles)
                {
                    _subtitle.Append(Gtk.Label.New(subtitle));
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
        _controller.OnInformationChanged += ControllerOnInformationChanged;
        base.Dispose();
    }
}
