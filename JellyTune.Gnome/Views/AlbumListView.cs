using JellyTune.Shared.Controls;

namespace JellyTune.Gnome.Views;

[GObject.Subclass<Gtk.Box>(qualifiedName: "JellyTuneAlbumListView")]
[Gtk.Template<Gtk.AssemblyResource>("JellyTune.Gnome.Blueprints.albumlist.ui")]
public partial class AlbumListView
{
    private AlbumlistController _controller;

    public static AlbumListView NewWithValues(AlbumlistController controller)
    {
        var obj = NewWithProperties([]);
        obj._controller = controller;
        obj.Append(ListView.NewWithValues(obj._controller));
        return obj;
    }
}