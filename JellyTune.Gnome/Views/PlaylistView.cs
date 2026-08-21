using Gtk.Internal;
using JellyTune.Shared.Controls;
using JellyTune.Gnome.Helpers;


namespace JellyTune.Gnome.Views;

[GObject.Subclass<Gtk.Box>(qualifiedName: "JellyTunePreferencesAlert")]
[Gtk.Template<Gtk.AssemblyResource>("JellyTune.Gnome.Blueprints.playlist.ui")]
public partial class PlaylistView
{
    private readonly PlaylistController _controller;

    private readonly ListView _listView;

    public PlaylistView(PlaylistController controller)
    {
        _controller = controller;
        Append(new Views.ListView(_controller));
    }
}