using JellyTune.Shared.Controls;


namespace JellyTune.Gnome.Views;

[GObject.Subclass<Gtk.Box>(qualifiedName: "JellyTunePlaylistView")]
[Gtk.Template<Gtk.AssemblyResource>("JellyTune.Gnome.Blueprints.playlist.ui")]
public partial class PlaylistView
{
    private PlaylistController _controller;

    private ListView _listView;

    public static PlaylistView NewWithValues(PlaylistController controller)
    {
        var obj = NewWithProperties([]);
        obj._controller = controller;
        obj.InitializeController();
        return obj;
    }
    
    private void InitializeController()
    {
        Append(ListView.NewWithValues(_controller));
    }
}