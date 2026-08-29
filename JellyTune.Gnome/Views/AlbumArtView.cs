using Gdk;
using GLib;
using GObject;
using Gtk;
using JellyTune.Gnome.Helpers;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Events;
using Dialog = Adw.Dialog;
using Spinner = Adw.Spinner;

namespace JellyTune.Gnome.Views;

[Subclass<Dialog>(qualifiedName: "JellyTuneAlbumArtView")]
[Template<AssemblyResource>("JellyTune.Gnome.Blueprints.album_art.ui")]
public partial class AlbumArtView
{
    private AlbumArtController _controller;
    
    [Connect] private Spinner _spinner;
    [Connect] private Revealer _results;
    
    [Connect] private Image _albumArt;
    [Connect] private Label _album;
    [Connect] private Label _artist;

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
                _artist.SetText(_controller.Album != null ? _controller.Album.Artist != null ? _controller.Album.Artist : string.Empty : string.Empty);
                _album.SetText(_controller.Album != null ? _controller.Album.Name != null ? _controller.Album.Name : string.Empty : string.Empty);
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
        
        using var bytes = Bytes.New(_controller.ArtWork);
        using var texture = Texture.NewFromBytes(bytes);
        _albumArt.SetFromPaintable(texture);
    }
    
    public override void Dispose()
    {
        _controller.OnAlbumArtChanged -= ControllerOnAlbumArtChanged;
        base.Dispose();
    }
}