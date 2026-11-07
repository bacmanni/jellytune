using Adw;
using GObject;
using Gtk;
using JellyTune.Gnome.Enums;
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
                _controller.AddTrackToPlaylist(row.TrackId, row.TrackIndex);
                if (_controller.PlayerService.IsPlaying())
                {
                    _controller.PlayerService.StartTrackAsync();
                }
                
                UpdateTrackState();
            }
        };
        
        //_playlistTracksList.AddController(clickGesture);
    }

    private void PlaylistTracksListOnRowActivated(ListBox sender, ListBox.RowActivatedSignalArgs args)
    {
        if (args.Row is SwipeRow row)
        {
            _ = _controller.PlayOrPauseTrackAsync(row.TrackRow.TrackId, row.TrackRow.TrackIndex);
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
                var state = _controller.PlayerService.GetTrackState(track.Id, track.Index);
                var row = TrackRow.NewWithValues(_controller.PlayerService, _controller.FileService, track, state,
                    [RowProperty.Extended, RowProperty.Queue]);
                var swipeRow = SwipeRow.NewWithTrackRow(row, new SwipeProperties()
                {
                    LeftStyle = "swipe-add-active",
                    LeftTitle = "Add track to queue",
                    RightStyle = "swipe-add-active",
                    RightTitle = "Add track to queue",
                });
                
                swipeRow.OnSwipedLeft += trackRow =>
                {
                    _controller.AddTrackToPlaylist(trackRow.TrackId, trackRow.TrackIndex);
                    UpdateTrackState();
                };

                swipeRow.OnSwipedRight += trackRow =>
                {
                    _controller.AddTrackToPlaylist(trackRow.TrackId, trackRow.TrackIndex);
                    UpdateTrackState();
                };
                
                _playlistTracksList.Append(swipeRow);
            }
        });
        
        _spinner.SetVisible(false);
        _results.SetVisible(true);
    }

    private void UpdateTrackState()
    {
        for (var i = 0; i < _controller.Tracks.Count; i++)
        {
            var row = _playlistTracksList.GetRowAtIndex(i) as SwipeRow;
            if (row == null)  continue;
            
            var state = _controller.PlayerService.GetTrackState(row.TrackRow.TrackId, row.TrackRow.TrackIndex);
            row.TrackRow.UpdateState(state);
        }
    }
    
    public override void Dispose()
    {
        _controller.OnPlaylistTracksStateChanged -= ControllerOnPlaylistTracksStateChanged;
        _playlistTracksList.OnRowActivated -= PlaylistTracksListOnRowActivated;
        base.Dispose();
    }
}