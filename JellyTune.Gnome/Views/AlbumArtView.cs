using Adw.Internal;
using JellyTune.Gnome.Helpers;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Events;

namespace JellyTune.Gnome.Views;

public partial class AlbumArtView : Adw.Dialog
{
    private readonly AlbumArtController _controller;
    
    [Gtk.Connect] private readonly Adw.Spinner _spinner;
    [Gtk.Connect] private readonly Gtk.Revealer _results;
    
    [Gtk.Connect] private readonly Gtk.Image _albumArt;
    [Gtk.Connect] private readonly Gtk.Label _album;
    [Gtk.Connect] private readonly Gtk.Label _artist;
    
    private AlbumArtView(Gtk.Builder builder) : base(
        new DialogHandle(builder.GetPointer("_root"), false))
    {
        builder.Connect(this);
    }
    
    public AlbumArtView(AlbumArtController controller) : this(GtkHelper.BuilderFromFile("album_art"))
    {
        _controller = controller;
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