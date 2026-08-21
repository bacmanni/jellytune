using JellyTune.Gnome.Helpers;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Events;
using DialogHandle = Adw.Internal.DialogHandle;
using ListBox = Gtk.ListBox;

namespace JellyTune.Gnome.Views;

[GObject.Subclass<Adw.Dialog>(qualifiedName: "JellyTuneArtistAlbumView")]
[Gtk.Template<Gtk.AssemblyResource>("JellyTune.Gnome.Blueprints.artist_album.ui")]
public partial class ArtistAlbumView
{
    private readonly ArtistAlbumController  _controller;

    [Gtk.Connect] private Adw.Spinner _spinner;
    [Gtk.Connect] private Gtk.Revealer _result;
    
    [Gtk.Connect] private Gtk.ListBox _albums;

    public ArtistAlbumView(ArtistAlbumController controller)
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
            if (GetRoot() is Gtk.Window win)
            {
                win.ActivateAction("win.open_album", GLib.Variant.NewString(row.AlbumId.ToString()));
            }
            
            Close();
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

    public override void Dispose()
    {
        _controller.OnAlbumsChanged -= ControllerOnAlbumsChanged;
        _albums.OnRowActivated -= AlbumsOnRowActivated;
        base.Dispose();
    }
}