using Gtk.Internal;
using JellyTune.Shared.Controls;
using JellyTune.Gnome.Helpers;

namespace JellyTune.Gnome.Views;

[GObject.Subclass<Gtk.Box>(qualifiedName: "JellyTuneAlbumListView")]
[Gtk.Template<Gtk.AssemblyResource>("JellyTune.Gnome.Blueprints.albumlist.ui")]
public partial class AlbumListView
{
    private readonly AlbumlistController _controller;

    private readonly ListView _listView;

    public AlbumListView(AlbumlistController controller)
    {
        _controller = controller;
        Append(new Views.ListView(_controller));
    }
}