using Adw;
using GObject;
using Gtk;
using JellyTune.Gnome.Helpers;
using JellyTune.Gnome.Models;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Events;
using ListBox = Gtk.ListBox;
using Spinner = Adw.Spinner;

namespace JellyTune.Gnome.Views;

[Subclass<ScrolledWindow>(qualifiedName: "JellyTunePlaylistTracks")]
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
        
        var clickGesture = GestureClick.New();
        clickGesture.SetPropagationPhase(Gtk.PropagationPhase.Capture);
        clickGesture.OnPressed += (sender, args) =>
        {
            var state = clickGesture.GetCurrentEventState();
            var isCtrlPressed = state.HasFlag(Gdk.ModifierType.ControlMask);

            if (!isCtrlPressed)
            {
                clickGesture.SetState(EventSequenceState.Denied);
                return;
            }

            clickGesture.SetState(EventSequenceState.Claimed);
            if (_playlistTracksList.GetRowAtY((int)args.Y) is TrackRow row)
            {
                if (!_controller.PlayerService.HasCustomPlaylist())
                {
                    _controller.PlayerService.StopTrack();
                    _controller.PlayerService.ClearTracks();
                }
                    
                _controller.AddTrackToPlaylist(row.TrackId);
                UpdateTrackState();
            }
        };
        
        _playlistTracksList.AddController(clickGesture);
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
            return;
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
                _playlistTracksList.Append(TrackRow.NewWithValues(_controller.PlayerService, _controller.FileService, track, state, [RowProperty.Extended, RowProperty.Queue]));
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