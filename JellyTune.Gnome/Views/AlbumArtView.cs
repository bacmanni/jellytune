using JellyTune.Gnome.Helpers;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Events;

namespace JellyTune.Gnome.Views;

[GObject.Subclass<Adw.Dialog>(qualifiedName: "JellyTuneAlbumArtView")]
[Gtk.Template<Gtk.AssemblyResource>("JellyTune.Gnome.Blueprints.album_art.ui")]
public partial class AlbumArtView
{
    private AlbumArtController _controller;
    
    [Gtk.Connect] private Adw.Spinner _spinner;
    [Gtk.Connect] private Gtk.Revealer _results;
    
    [Gtk.Connect] private Gtk.Image _albumArt;
    [Gtk.Connect] private Gtk.Label _album;
    [Gtk.Connect] private Gtk.Label _artist;

    public static AlbumArtView NewWithValues(AlbumArtController controller)
    {
        var obj = NewWithProperties([]);
        obj._controller = controller;
        obj.InitializeController();
        return obj;
    }

    private void InitializeController()
    {
        _controller.OnAlbumArtChanged += ControllerOnAlbumArtChanged;
        _results.SetRevealChild(false);
        _spinner.SetVisible(true);
    }

    private void ControllerOnAlbumArtChanged(object? sender, AlbumArtArgs e)
    {
        var isLoading = e.IsLoading;
        
        GtkHelper.GtkDispatch(() =>
        {
            if (isLoading)
            {
                _artist.SetText(_controller.Album.Artist);
                _album.SetText(_controller.Album.Name);
                _results.SetRevealChild(false);
                _spinner.SetVisible(true);
                return;
            }
            
            _spinner.SetVisible(false);
            UpdateArtwork();
            _results.SetRevealChild(true);
        });
    }

    private void UpdateArtwork()
    {
        _albumArt.Clear();
        if (_controller.ArtWork == null) return;
        
        using var bytes = GLib.Bytes.New(_controller.ArtWork);
        using var texture = Gdk.Texture.NewFromBytes(bytes);
        _albumArt.SetFromPaintable(texture);
    }
    
    public override void Dispose()
    {
        _controller.OnAlbumArtChanged -= ControllerOnAlbumArtChanged;
        base.Dispose();
    }
}