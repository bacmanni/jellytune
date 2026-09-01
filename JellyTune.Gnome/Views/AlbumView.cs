using Adw;
using Gdk;
using GLib;
using GObject;
using Gtk;
using JellyTune.Gnome.Helpers;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Events;
using GestureClick = Gtk.GestureClick;
using ListBox = Gtk.ListBox;
using Spinner = Adw.Spinner;

namespace JellyTune.Gnome.Views;

[Subclass<ScrolledWindow>(qualifiedName: "JellyTuneAlbumView")]
[Template<AssemblyResource>("JellyTune.Gnome.Blueprints.album.ui")]
public partial class AlbumView
{
    private AlbumController _controller;
    private AlbumArtController _albumArtController;
    
    [Connect] private Image _albumArt;
    [Connect] private Label _artist;
    [Connect] private Label _album;
    [Connect] private Label _trackCount;
    [Connect] private Label _albumDuration;
    [Connect] private Label _albumYear;
    [Connect] private ListBox _tracks;
    [Connect] private Spinner _spinner;
    [Connect] private Clamp _result;

    public static AlbumView NewWithValues(AlbumController controller)
    {
        var obj = NewWithProperties([]);
        obj._controller = controller;
        obj._albumArtController = new AlbumArtController(obj._controller.JellyTuneApiService);
        obj.InitializeController();
        return obj;
    }

    private void InitializeController()
    {
        _controller.OnAlbumChanged += ControllerOnAlbumChanged;
        _tracks.OnRowSelected += TracksOnRowSelected;
        _tracks.OnRowActivated += TracksOnRowActivated;
        
        var click = GestureClick.New();
        click.OnPressed += ClickOnPressed;
        _albumArt.AddController(click);
    }

    private void ClickOnPressed(GestureClick sender, GestureClick.PressedSignalArgs args)
    {
        if (_controller.Artwork == null || _controller.Album?.Id == null) return;
        
        var albumArtDialog = AlbumArtView.NewWithValues(_albumArtController);
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
        
        _artist.SetText(_controller.Album != null ? _controller.Album.Artist != null ? _controller.Album.Artist : "No artist" : "No artist");
        _album.SetText(_controller.Album != null ? _controller.Album.Name != null ? _controller.Album.Name : "No album" : "No album");
        _trackCount.SetText($"{_controller.Tracks.Count.ToString()} tracks");
        
        if (_controller.Album != null && _controller.Album.Runtime != null)
            _albumDuration.SetText($"{_controller.Album.Runtime.Value.TotalMinutes:F0}m");
        
        if (_controller.Album != null && _controller.Album.Year != null)
            _albumYear.SetText(_controller.Album.Year.Value.ToString());
    }

    private void UpdateArtwork()
    {
        if (_controller.Artwork != null)
        {
            _albumArt.Clear();
            using var bytes = Bytes.New(_controller.Artwork);
            using var texture = Texture.NewFromBytes(bytes);
            _albumArt.SetFromPaintable(texture);
        }
    }
    
    private void UpdateTracks()
    {
        _tracks.RemoveAll();
        foreach (var track in _controller.Tracks)
        {
            var state = _controller.PlayerService.GetTrackState(track.Id);
            var row = TrackRow.NewWithValues(_controller.FileService, track, state);
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