using Adw;
using GObject;
using Gtk;
using JellyTune.Gnome.Helpers;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Events;
using ListBox = Gtk.ListBox;
using Spinner = Adw.Spinner;

namespace JellyTune.Gnome.Views;

[Subclass<Box>(qualifiedName: "JellyTunePlaylistTracks")]
[Template<AssemblyResource>("JellyTune.Gnome.Blueprints.playlist_tracks.ui")]
public partial class PlaylistTracksView
{
    private PlaylistTracksController _controller;
    
    [Connect] private Spinner _spinner;
    [Connect] private Clamp _results;
    [Connect] private ListBox _playlistTracksList;

    public static PlaylistTracksView NewWithValues(PlaylistTracksController controller)
    {
        var obj = NewWithProperties([]);
        obj._controller = controller;
        obj.InitializeController();
        return obj;
    }

    private void InitializeController()
    {
        _controller.OnPlaylistTracksStateChanged += ControllerOnPlaylistTracksStateChanged;
        _playlistTracksList.OnRowActivated += PlaylistTracksListOnRowActivated;
        _results.SetVisible(false);
        _spinner.SetVisible(true);
    }

    private void PlaylistTracksListOnRowActivated(ListBox sender, ListBox.RowActivatedSignalArgs args)
    {
        if (args.Row is TrackRow row)
        {
            _ = _controller.PlayOrPauseTrackAsync(row.TrackId);
        }
    }

    private void ControllerOnPlaylistTracksStateChanged(object? sender, PlaylistTracksStateArgs e)
    {
        if (e.Loading)
        {
            _results.SetVisible(false);
            _spinner.SetVisible(true);
            return;
        }
     
        var updateTrackState = e.UpdateTrackState;
        var trackId = e.SelectedTrackId;
        
        GtkHelper.GtkDispatch(() =>
        {
            if (updateTrackState)
            {
                if (trackId.HasValue)
                    UpdateTrackState();
                
                return;
            }
        
            _playlistTracksList.RemoveAll();
            foreach (var track in _controller.Tracks)
            {
                var state = _controller.PlayerService.GetTrackState(track.Id);
                _playlistTracksList.Append(TrackRow.NewWithValues(_controller.FileService, track, state, true));
            }
        });
        
        _spinner.SetVisible(false);
        _results.SetVisible(true);
    }

    private void UpdateTrackState()
    {
        for (var i = 0; i < _controller.Tracks.Count; i++)
        {
            var row = _playlistTracksList.GetRowAtIndex(i) as TrackRow;
            if (row == null)  continue;
            
            var state = _controller.PlayerService.GetTrackState(row.TrackId);
            row.UpdateState(state);
        }
    }
    
    public override void Dispose()
    {
        _controller.OnPlaylistTracksStateChanged -= ControllerOnPlaylistTracksStateChanged;
        _playlistTracksList.OnRowActivated -= PlaylistTracksListOnRowActivated;
        base.Dispose();
    }
}