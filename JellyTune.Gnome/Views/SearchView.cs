using Gtk.Internal;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Enums;
using JellyTune.Gnome.Helpers;

namespace JellyTune.Gnome.Views;

public class SearchView : Gtk.ScrolledWindow
{
    private readonly SearchController _controller;

    [Gtk.Connect] private readonly Adw.Spinner _spinner;
    [Gtk.Connect] private readonly Adw.StatusPage _noresults;
    [Gtk.Connect] private readonly Adw.Clamp _results;
    [Gtk.Connect] private readonly Gtk.ListBox _searchList;
    [Gtk.Connect] private readonly Adw.StatusPage _startup;
    
    private readonly Gio.ListStore _searchListItems;
    
    private SearchView(Gtk.Builder builder) : base(
        new ScrolledWindowHandle(builder.GetPointer("_root"), false))
    {
        builder.Connect(this);
    }

    private void SetSpinner(bool? show = null, int? results = null)
    {
        if (!show.HasValue)
        {
            _startup.SetVisible(true);
            _noresults.Hide();
            _results.Hide();
            _spinner.Hide();
            return;
        }
            
        _startup.SetVisible(false);
        
        if (show.Value)
        {
            _noresults.Hide();
            _results.Hide();
            _spinner.Show();
        }
        else
        {
            _spinner.Hide();
            
            if (results.HasValue && results.Value > 0)
                _results.Show();
            else
                _noresults.Show();
        }
    }
    
    public SearchView(SearchController controller) : this(Blueprint.BuilderFromFile("search"))
    {
        _controller = controller;
        _controller.OnSearchStateChanged += (sender, args) =>
        {
            if (args.Open)
                SetSpinner();
            
            if (args.Start)
                SetSpinner(true);

            if (args.Updated)
                UpdateSearch();
        };

        _searchList.OnRowActivated += (sender, args) =>
        {
            var row = args.Row as SearchRow;
            if (row != null)
            {
                Guid? trackId = row.Type == SearchType.Track ? row.Id : null;
                _controller.OpenAlbum(row.AlbumId, trackId);
            }
        };
    }

    private void UpdateSearch()
    {
        if (_controller.Results.Count > 0)
        {
            _searchList.RemoveAll();
            foreach (var result in _controller.Results)
            {
                _searchList.Append(new SearchRow(_controller.GetFileService(), result));
            }
        }
        
        SetSpinner(false, _controller.Results.Count);
    }
}