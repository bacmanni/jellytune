using Gtk.Internal;
using JellyTune.Gnome.Helpers;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Events;
using DialogHandle = Adw.Internal.DialogHandle;
using ListBox = Gtk.ListBox;

namespace JellyTune.Gnome.Views;

public class ArtistAlbumView : Adw.Dialog
{
    private readonly ArtistAlbumController  _controller;

    [Gtk.Connect] private readonly Adw.Spinner _spinner;
    [Gtk.Connect] private readonly Gtk.Revealer _result;
    
    [Gtk.Connect] private readonly Gtk.ListBox _albums;
    
    private ArtistAlbumView(Gtk.Builder builder) : base(
        new DialogHandle(builder.GetPointer("_root"), false))
    {
        builder.Connect(this);
    }

    public ArtistAlbumView(ArtistAlbumController controller) : this(GtkHelper.BuilderFromFile("artist_album"))
    {
        _controller = controller;
        _controller.OnAlbumsChanged += ControllerOnAlbumsChanged;
        _albums.OnRowActivated += AlbumsOnRowActivated;
        
        _result.SetVisible(false);
        _spinner.SetVisible(true);
    }

    private void AlbumsOnRowActivated(ListBox sender, ListBox.RowActivatedSignalArgs args)
    {
        if (args.Row is AlbumRow row)
        {
            Close();

            if (GetRoot() is Gtk.Window win)
            {
                win.ActivateAction("win.open_album", GLib.Variant.NewString(row.AlbumId.ToString()));
            }
        }
    }

    private void ControllerOnAlbumsChanged(object? sender, ArtistAlbumArgs e)
    {
        var isLoading = e.IsLoading;
        GtkHelper.GtkDispatch(() =>
        {
            if (isLoading)
            {
                _result.SetVisible(false);
                _spinner.SetVisible(true);
                return;
            }

            _spinner.SetVisible(false);
            _albums.RemoveAll();
            foreach (var album in _controller.Albums)
            {
                var row = new AlbumRow(_controller.FileService, album);
                _albums.Append(row);
            }
            
            _result.SetVisible(true);
            _result.SetRevealChild(true);
        });
    }
}