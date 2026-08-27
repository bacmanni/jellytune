using JellyTune.Shared.Controls;
using JellyTune.Shared.Enums;
using JellyTune.Gnome.Helpers;
using JellyTune.Shared.Events;
using ListBox = Gtk.ListBox;

namespace JellyTune.Gnome.Views;

[GObject.Subclass<Gtk.ScrolledWindow>(qualifiedName: "JellyTuneSearchView")]
[Gtk.Template<Gtk.AssemblyResource>("JellyTune.Gnome.Blueprints.search.ui")]
public partial class SearchView
{
    private SearchController _controller;

    [Gtk.Connect] private Adw.Spinner _spinner;
    [Gtk.Connect] private Adw.StatusPage _noresults;
    [Gtk.Connect] private Adw.Clamp _results;
    [Gtk.Connect] private Gtk.ListBox _searchList;
    [Gtk.Connect] private Adw.StatusPage _startup;

    public static SearchView NewWithValues(SearchController controller)
    {
        var obj = NewWithProperties([]);
        obj._controller = controller;
        obj.InitializeController();
        return obj;
    }

    private void UpdateResults(bool? show = null, int? results = null)
    {
        _noresults.SetVisible(false);
        _results.SetVisible(false);
        _spinner.SetVisible(false);
        
        // Startup state
        if (!show.HasValue)
        {
            _startup.SetVisible(true);
        }
        else
        {
            _startup.SetVisible(false);

            // Spinner should be shown and nothing else
            if (show == true)
            {
                _spinner.SetVisible(true);
            }
            else
            {
                _searchList.RemoveAll();
                if (_controller.Results.Count > 0)
                {
                    foreach (var result in _controller.Results)
                    {
                        _searchList.Append(SearchRow.NewWithValues(_controller.FileService, result));
                    }
                    
                    _results.SetVisible(true);
                }
                else
                {
                    _noresults.SetVisible(true);
                }
            }
        }
    }
    
    private void InitializeController()
    {
        _controller.OnSearchStateChanged += ControllerOnOnSearchStateChanged;
        _searchList.OnRowActivated += SearchListOnOnRowActivated;
    }

    private void SearchListOnOnRowActivated(ListBox sender, ListBox.RowActivatedSignalArgs args)
    {
        var row = args.Row as SearchRow;
        if (row != null)
        {
            Guid? trackId = row.Type == SearchType.Track ? row.Id : null;
            _controller.OpenAlbum(row.AlbumId, trackId);
        }
    }

    private void ControllerOnOnSearchStateChanged(object? sender, SearchStateArgs args)
    {
        GtkHelper.GtkDispatch(() =>
        {
            if (args.Open)
                UpdateResults();
        
            if (args.Start)
                UpdateResults(true);

            if (args.Updated)
                UpdateResults(false, _controller.Results.Count);
        });
    }

    public override void Dispose()
    {
        _controller.OnSearchStateChanged -= ControllerOnOnSearchStateChanged;
        _searchList.OnRowActivated -= SearchListOnOnRowActivated;
        base.Dispose();
    }
}