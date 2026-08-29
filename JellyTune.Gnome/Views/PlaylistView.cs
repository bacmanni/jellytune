using GObject;
using Gtk;
using JellyTune.Shared.Controls;

namespace JellyTune.Gnome.Views;

[Subclass<Box>(qualifiedName: "JellyTunePlaylistView")]
[Template<AssemblyResource>("JellyTune.Gnome.Blueprints.playlist.ui")]
public partial class PlaylistView
{
    private PlaylistController _controller;

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