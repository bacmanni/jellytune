using Gtk.Internal;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using JellyTune.Shared.Models;
using JellyTune.Gnome.Helpers;
using ListBox = Gtk.ListBox;

namespace JellyTune.Gnome.Views;

public class QueueListView : Gtk.ScrolledWindow
{
    private readonly QueueListController  _controller;
    
    [Gtk.Connect] private readonly Gtk.ListBox _queueList;

    private QueueListView(Gtk.Builder builder) : base(
        new ScrolledWindowHandle(builder.GetPointer("_root"), false))
    {
        builder.Connect(this);
    }

    public QueueListView(QueueListController controller) : this(Blueprint.BuilderFromFile("queue_list"))
    {
        _controller = controller;
        _controller.OnQueueUpdated += ControllerOnQueueUpdated;
        _queueList.OnRowActivated += QueueListOnRowActivated;
        _controller.GetPlayerService().OnPlayerStateChanged += OnPlayerStateChanged;
    }

    private void OnPlayerStateChanged(object? sender, PlayerStateArgs args)
    {
        if (args.State is PlayerState.Playing or PlayerState.Paused or PlayerState.Starting or PlayerState.Selected)
        {
            UpdateRowState(args.SelectedTrack.Id, args.State);
        }
    }

    private void QueueListOnRowActivated(ListBox sender, ListBox.RowActivatedSignalArgs args)
    {
        var row = args.Row as TrackRow;
        if (row is null)
            return;
                
        _controller.GetPlayerService().StartTrackAsync(row.TrackId);
    }

    private void ControllerOnQueueUpdated(object? sender, QueueArgs e)
    {
        _queueList.RemoveAll();
        foreach (var track in _controller.Tracks)
        {
            var state = _controller.GetPlayerService().GetTrackState(track.Id);
            _queueList.Append(new TrackRow(_controller.GetFileService(), track, state, true));
        }
    }

    private void UpdateRowState(Guid trackId, PlayerState state)
    {
        for (var i = 0; i < _controller.Tracks.Count; i++)
        {
            var row = _queueList.GetRowAtIndex(i) as TrackRow;
            if (row == null)  continue;

            row.UpdateState(row.TrackId == trackId ? state : PlayerState.None);
        }
    }

    public override void Dispose()
    {
        _controller.OnQueueUpdated -= ControllerOnQueueUpdated;
        _queueList.OnRowActivated -= QueueListOnRowActivated;
        _controller.GetPlayerService().OnPlayerStateChanged -= OnPlayerStateChanged;
        base.Dispose();
    }
}