using Gtk.Internal;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Events;
using JellyTune.Gnome.Helpers;
using GestureClick = Gtk.GestureClick;
using ListBox = Gtk.ListBox;

namespace JellyTune.Gnome.Views;

[GObject.Subclass<Gtk.ScrolledWindow>(qualifiedName: "JellyTuneAlbumView")]
[Gtk.Template<Gtk.AssemblyResource>("JellyTune.Gnome.Blueprints.album.ui")]
public partial class AlbumView
{
    private readonly AlbumController _controller;
    private readonly AlbumArtController _albumArtController;
    
    [Gtk.Connect] private Gtk.Image _albumArt;
    [Gtk.Connect] private Gtk.Label _artist;
    [Gtk.Connect] private Gtk.Label _album;
    [Gtk.Connect] private Gtk.Label _trackCount;
    [Gtk.Connect] private Gtk.Label _albumDuration;
    [Gtk.Connect] private Gtk.Label _albumYear;
    [Gtk.Connect] private Gtk.ListBox _tracks;
    [Gtk.Connect] private Adw.Spinner _spinner;
    [Gtk.Connect] private Adw.Clamp _result;

    public AlbumView(AlbumController controller)
    {
        _controller = controller;
        _albumArtController = new AlbumArtController(_controller.JellyTuneApiService);
        _controller.OnAlbumChanged += ControllerOnAlbumChanged;
    }

    partial void Initialize()
    {
        _tracks.OnRowSelected += TracksOnRowSelected;
        _tracks.OnRowActivated += TracksOnRowActivated;
        
        var click = Gtk.GestureClick.New();
        click.OnPressed += ClickOnPressed;
        _albumArt.AddController(click);
    }

    private void ClickOnPressed(GestureClick sender, GestureClick.PressedSignalArgs args)
    {
        if (_controller.Artwork == null || _controller.Album?.Id == null) return;
        
        var albumArtDialog = new AlbumArtView(_albumArtController);
        albumArtDialog.Present(this);
        _ = _albumArtController.OpenAsync(_controller.Album);
    }

    private void ControllerOnAlbumChanged(object? sender, AlbumStateArgs args)
    {
        var updateAlbum = args.UpdateAlbum;
        var updateTracks = args.UpdateTracks;
        var updateArtwork = args.UpdateArtwork;
        var updateTrackState = args.UpdateTrackState;

        GtkHelper.GtkDispatch(() =>
        {
            if (!updateAlbum && !updateTracks && !updateArtwork && !updateTrackState)
            {
                SetSpinner(true);
                return;
            }

            if (updateAlbum)
                UpdateAlbum();

            if (updateTracks)
                UpdateTracks();

            if (updateArtwork)
                UpdateArtwork();

            if (updateTrackState)
                UpdateTrackState();
        });
    }

    private void TracksOnRowActivated(ListBox sender, ListBox.RowActivatedSignalArgs args)
    {
        if (args.Row is TrackRow row)
        { 
            _ = _controller.PlayOrPauseTrackAsync(row.TrackId);
        }
    }

    private void TracksOnRowSelected(ListBox sender, ListBox.RowSelectedSignalArgs args)
    {
        if (args.Row is TrackRow row)
        {
            _controller.SelectTrack(row.TrackId);
        }
    }

    private void SetSpinner(bool show)
    {
        if (show)
        {
            _result.SetVisible(false);
            _spinner.SetVisible(true);
        }
        else
        {
            _spinner.SetVisible(false);
            _result.SetVisible(true);
        }
    }
    
    private void UpdateAlbum()
    {
        // Clear artwork
        _albumArt.Clear();

        SetSpinner(false);
        
        _artist.SetText(_controller.Album.Artist);
        _album.SetText(_controller.Album.Name);
        _trackCount.SetText($"{_controller.Tracks.Count.ToString()} tracks");
        
        if (_controller.Album?.Runtime != null)
            _albumDuration.SetText($"{_controller.Album.Runtime.Value.TotalMinutes:F0}m");
        
        if (_controller.Album?.Year != null)
            _albumYear.SetText(_controller.Album.Year.Value.ToString());
    }

    private void UpdateArtwork()
    {
        if (_controller.Artwork != null)
        {
            _albumArt.Clear();
            using var bytes = GLib.Bytes.New(_controller.Artwork);
            using var texture = Gdk.Texture.NewFromBytes(bytes);
            _albumArt.SetFromPaintable(texture);
        }
    }
    
    private void UpdateTracks()
    {
        _tracks.RemoveAll();
        foreach (var track in _controller.Tracks)
        {
            var state = _controller.PlayerService.GetTrackState(track.Id);
            var row = new TrackRow(_controller.FileService, track, state);
            _tracks.Append(row);
        }
    }

    private void UpdateTrackState()
    {
        for (var i = 0; i < _controller.Tracks.Count; i++)
        {
            var row = _tracks.GetRowAtIndex(i) as TrackRow;
            if (row == null)  continue;
            
            var state = _controller.PlayerService.GetTrackState(row.TrackId);
            row.UpdateState(state);
        }
    }

    public override void Dispose()
    {
        _tracks.OnRowSelected -= TracksOnRowSelected;
        _tracks.OnRowActivated -= TracksOnRowActivated;
        _controller.OnAlbumChanged -= ControllerOnAlbumChanged;
        base.Dispose();
    }
}