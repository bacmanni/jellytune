using Gtk.Internal;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using JellyTune.Shared.Models;
using JellyTune.Gnome.Helpers;
using ListBox = Gtk.ListBox;

namespace JellyTune.Gnome.Views;

[GObject.Subclass<Gtk.ScrolledWindow>(qualifiedName: "JellyTuneQueueListView")]
[Gtk.Template<Gtk.AssemblyResource>("JellyTune.Gnome.Blueprints.queue_list.ui")]
public partial class QueueListView
{
    private readonly QueueListController  _controller;
    
    [Gtk.Connect] private Gtk.ListBox _queueList;

    public QueueListView(QueueListController controller)
    {
        _controller = controller;
        _controller.OnQueueUpdated += ControllerOnQueueUpdated;
        _controller.PlayerService.OnPlayerStateChanged += OnPlayerStateChanged;
    }

    partial void Initialize()
    {
        _queueList.OnRowActivated += QueueListOnRowActivated;
    }

    private void OnPlayerStateChanged(object? sender, PlayerStateArgs args)
    {
        if (args.State is PlayerState.Playing or PlayerState.Paused or PlayerState.None)
        {
            UpdateRowState();
        }
    }

    private void QueueListOnRowActivated(ListBox sender, ListBox.RowActivatedSignalArgs args)
    {
        var row = args.Row as TrackRow;
        if (row is null)
            return;
                
        _controller.PlayerService.StartTrackAsync(row.TrackId);
    }

    private void ControllerOnQueueUpdated(object? sender, QueueArgs e)
    {
        GtkHelper.GtkDispatch(() =>
        {
            _queueList.RemoveAll();
            foreach (var track in _controller.Tracks)
            {
                var state = _controller.PlayerService.GetTrackState(track.Id);
                _queueList.Append(new TrackRow(_controller.FileService, track, state, true));
            } 
        });
    }

    private void UpdateRowState()
    {
        GtkHelper.GtkDispatch(() =>
        {
            for (var i = 0; i < _controller.Tracks.Count; i++)
            {
                var row = _queueList.GetRowAtIndex(i) as TrackRow;
                if (row == null)  continue;

                var state = _controller.PlayerService.GetTrackState(row.TrackId);
                row.UpdateState(state);
            }
        });
    }

    public override void Dispose()
    {
        _controller.OnQueueUpdated -= ControllerOnQueueUpdated;
        _queueList.OnRowActivated -= QueueListOnRowActivated;
        _controller.PlayerService.OnPlayerStateChanged -= OnPlayerStateChanged;
        base.Dispose();
    }
}