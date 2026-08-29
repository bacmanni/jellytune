using Adw;
using GObject;
using Gtk;
using JellyTune.Gnome.Helpers;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using ListBox = Gtk.ListBox;
using Spinner = Adw.Spinner;

namespace JellyTune.Gnome.Views;

[Subclass<ScrolledWindow>(qualifiedName: "JellyTuneSearchView")]
[Template<AssemblyResource>("JellyTune.Gnome.Blueprints.search.ui")]
public partial class SearchView
{
    private SearchController _controller;

    [Connect] private Spinner _spinner;
    [Connect] private StatusPage _noresults;
    [Connect] private Clamp _results;
    [Connect] private ListBox _searchList;
    [Connect] private StatusPage _startup;

    public static SearchView NewWithValues(SearchController controller)
    {
        var obj = NewWithProperties([]);
        obj._controller = controller;
        obj.InitializeController();
        return obj;
    }

    private void UpdateResults(bool? show = null)
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
            if (show.Value)
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
                UpdateResults(false);
        });
    }

    public override void Dispose()
    {
        _controller.OnSearchStateChanged -= ControllerOnOnSearchStateChanged;
        _searchList.OnRowActivated -= SearchListOnOnRowActivated;
        base.Dispose();
    }
}