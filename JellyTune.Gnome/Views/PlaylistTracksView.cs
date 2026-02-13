using Gtk.Internal;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Events;
using JellyTune.Gnome.Helpers;
using ListBox = Gtk.ListBox;

namespace JellyTune.Gnome.Views;

public class PlaylistTracksView : Gtk.Box
{
    private readonly PlaylistTracksController _controller;
    
    [Gtk.Connect] private readonly Adw.Spinner _spinner;
    [Gtk.Connect] private readonly Adw.Clamp _results;
    [Gtk.Connect] private readonly Gtk.ListBox _playlistTracksList;
    
    private PlaylistTracksView(Gtk.Builder builder) : base(
        new BoxHandle(builder.GetPointer("_root"), false))
    {
        builder.Connect(this);
    }

    public PlaylistTracksView(PlaylistTracksController controller) : this(Blueprint.BuilderFromFile("playlist_tracks"))
    {
        _controller = controller;
        _controller.OnPlaylistTracksStateChanged += ControllerOnPlaylistTracksStateChanged;
        _playlistTracksList.OnRowActivated += PlaylistTracksListOnRowActivated;
        
        _results.SetVisible(false);
        _spinner.SetVisible(true);
    }

    private void PlaylistTracksListOnRowActivated(ListBox sender, ListBox.RowActivatedSignalArgs args)
    {
        if (args.Row is TrackRow row)
        {
            _controller.PlayOrPauseTrackAsync(row.TrackId);
        }
    }

    private void ControllerOnPlaylistTracksStateChanged(object? sender, PlaylistTracksStateArgs e)
    {
        GLib.MainContext.Default().InvokeFull(0, () =>
        {
            if (e.Loading)
            {
                _results.SetVisible(false);
                _spinner.SetVisible(true);
                return false;
            }

            if (e.UpdateTrackState)
            {
                UpdateTrackState(e.SelectedTrackId!.Value);
                return false;
            }

            _playlistTracksList.RemoveAll();

            foreach (var track in _controller.Tracks)
            {
                var state = _controller.PlayerService.GetTrackState(track.Id);
                _playlistTracksList.Append(new TrackRow(_controller.FileService, track, state, true));
            }

            _spinner.SetVisible(false);
            _results.SetVisible(true);
            return false;
        });
    }

    private void UpdateTrackState(Guid trackId)
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