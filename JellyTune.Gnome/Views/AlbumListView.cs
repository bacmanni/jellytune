using Gtk.Internal;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Events;
using JellyTune.Gnome.Helpers;
using JellyTune.Gnome.Models;
using SignalListItemFactory = Gtk.SignalListItemFactory;

namespace JellyTune.Gnome.Views;

public class AlbumListView : Gtk.Box
{
    private readonly AlbumlistController _controller;

    private readonly ListView _listView;
    
    private AlbumListView(Gtk.Builder builder) : base(
        new BoxHandle(builder.GetPointer("_root"), false))
    {
        builder.Connect(this);
    }

    public AlbumListView(AlbumlistController controller) : this(Blueprint.BuilderFromFile("albumlist"))
    {
        _controller = controller;
        Append(new Views.ListView(_controller.GetListController()));
    }
}