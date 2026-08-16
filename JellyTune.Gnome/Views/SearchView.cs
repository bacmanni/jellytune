using Gtk.Internal;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Enums;
using JellyTune.Gnome.Helpers;
using JellyTune.Shared.Events;
using SoundFlow.Utils;
using ListBox = Gtk.ListBox;

namespace JellyTune.Gnome.Views;

public class SearchView : Gtk.ScrolledWindow
{
    private readonly SearchController _controller;

    [Gtk.Connect] private readonly Adw.Spinner _spinner;
    [Gtk.Connect] private readonly Adw.StatusPage _noresults;
    [Gtk.Connect] private readonly Adw.Clamp _results;
    [Gtk.Connect] private readonly Gtk.ListBox _searchList;
    [Gtk.Connect] private readonly Adw.StatusPage _startup;

    private SearchView(Gtk.Builder builder) : base(
        new ScrolledWindowHandle(builder.GetPointer("_root"), false))
    {
        builder.Connect(this);
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
                        _searchList.Append(new SearchRow(_controller.FileService, result));
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
    
    public SearchView(SearchController controller) : this(GtkHelper.BuilderFromFile("search"))
    {
        _controller = controller;
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