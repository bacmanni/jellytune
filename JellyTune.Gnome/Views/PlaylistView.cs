using Gtk.Internal;
using JellyTune.Shared.Controls;
using JellyTune.Gnome.Helpers;


namespace JellyTune.Gnome.Views;

public class PlaylistView : Gtk.Box
{
    private readonly PlaylistController _controller;

    private readonly ListView _listView;
    
    private PlaylistView(Gtk.Builder builder) : base(
        new BoxHandle(builder.GetPointer("_root"), false))
    {
        builder.Connect(this);
    }

    public PlaylistView(PlaylistController controller) : this(Blueprint.BuilderFromFile("playlist"))
    {
        _controller = controller;
        Append(new Views.ListView(_controller));
    }
}