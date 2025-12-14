using Gtk.Internal;
using JellyTune.Gnome.Helpers;
using JellyTune.Gnome.Models;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Events;

namespace JellyTune.Gnome.Views;

public class ListView : Gtk.Box
{
    private readonly ListController _controller;
    
    [Gtk.Connect] private readonly Adw.Spinner _loader;
    [Gtk.Connect] private readonly Gtk.Box _results;
    
    [Gtk.Connect] private readonly Gtk.ListView _list;
    [Gtk.Connect] private readonly Gtk.ScrolledWindow _listWindow;
    private readonly Gtk.SignalListItemFactory _listFactory;
    
    [Gtk.Connect] private readonly Gtk.GridView _grid;
    [Gtk.Connect] private readonly Gtk.ScrolledWindow _gridWindow;
    private readonly Gtk.SignalListItemFactory _gridFactory;
    
    private readonly Gio.ListStore _listItems;
    
    private ListView(Gtk.Builder builder) : base(
        new BoxHandle(builder.GetPointer("_root"), false))
    {
        builder.Connect(this);
    }
    
    public ListView(ListController controller) : this(Blueprint.BuilderFromFile("list"))
    {
        _controller = controller;
        _controller.OnListChanged += ControllerOnListChanged;

        var configuration = _controller.GetConfigurationService().Get();
        _list.SetShowSeparators(configuration.ShowListSeparator);
        _controller.GetConfigurationService().Saved += OnSaved;

        _listItems = Gio.ListStore.New(ListRow.GetGType());
        var selectionModel = Gtk.NoSelection.New(_listItems);
        
        //List
        _listFactory = Gtk.SignalListItemFactory.New();
        _listFactory.OnSetup += ListFactoryOnSetup;
        _listFactory.OnBind += ListFactoryOnBind;
        _listFactory.OnUnbind += ListFactoryOnUnbind;
        _list.SetFactory(_listFactory);
        _list.SetModel(selectionModel);
        _list.OnActivate += (_, args) =>
        {
            if (_listItems.GetObject(args.Position) is ListRow row)
                _controller.OpenItem(row.Id);
        };
        
        // Grid
        _gridFactory = Gtk.SignalListItemFactory.New();
        _gridFactory.OnSetup += GridFactoryOnSetup;
        _gridFactory.OnBind += GridFactoryOnBind;
        _gridFactory.OnUnbind += GridFactoryOnUnbind;
        _grid.SetFactory(_gridFactory);
        _grid.SetModel(selectionModel);
        _grid.OnActivate += (_, args) =>
        {
            if (_listItems.GetObject(args.Position) is ListRow row)
                _controller.OpenItem(row.Id);
        };
    }

    private void GridFactoryOnUnbind(Gtk.SignalListItemFactory sender, Gtk.SignalListItemFactory.UnbindSignalArgs args)
    {
        var listItem = args.Object as Gtk.ListItem;
        if (listItem is null)
        {
            return;
        }
        
        var template = listItem.Child as GridItem;
        if (template is null)
        {
            return;
        }

        template.Clear();
    }

    private void ListFactoryOnUnbind(Gtk.SignalListItemFactory sender, Gtk.SignalListItemFactory.UnbindSignalArgs args)
    {
        var listItem = args.Object as Gtk.ListItem;
        if (listItem is null)
        {
            return;
        }

        var template = listItem.Child as ListItem;
        if (template is null)
        {
            return;
        }

        template.Clear();
    }

    private void ControllerOnListChanged(object? sender, ListStateArgs args)
    {
        if (args.Items is not null)
        {
            _listItems.RemoveAll();
            foreach (var item in args.Items)
            {
                _listItems.Append(new ListRow(item));   
            }
        }
            
        if (args.IsLoading)
        {
            _results.SetVisible(false);
            _loader.SetVisible(true);
        }
        else
        {
            _loader.SetVisible(false);
            _results.SetVisible(true);
        }
    }

    private void GridFactoryOnBind(Gtk.SignalListItemFactory sender, Gtk.SignalListItemFactory.BindSignalArgs args)
    {
        var listItem = args.Object as Gtk.ListItem;
        if (listItem is null)
        {
            return;
        }
        
        var template = listItem.Child as GridItem;
        if (template is null)
        {
            return;
        }

        if (listItem.Item is ListRow item)
            template.Bind(item);
    }

    private void GridFactoryOnSetup(Gtk.SignalListItemFactory sender, Gtk.SignalListItemFactory.SetupSignalArgs args)
    {
        var listItem = args.Object as Gtk.ListItem;
        if (listItem is null)
        {
            return;
        }
        
        listItem.SetChild(new GridItem(_controller.GetFileService()));
    }

    private void ListFactoryOnBind(Gtk.SignalListItemFactory sender, Gtk.SignalListItemFactory.BindSignalArgs args)
    {
        var listItem = args.Object as Gtk.ListItem;
        if (listItem is null)
        {
            return;
        }

        var template = listItem.Child as ListItem;
        if (template is null)
        {
            return;
        }

        if (listItem.Item is ListRow item)
            template.Bind(item);
    }

    private void ListFactoryOnSetup(Gtk.SignalListItemFactory sender, Gtk.SignalListItemFactory.SetupSignalArgs args)
    {
        var listItem = args.Object as Gtk.ListItem;
        if (listItem is null)
        {
            return;
        }

        listItem.SetChild(new ListItem(_controller.GetFileService()));
    }

    public override void Dispose()
    {
        _controller.OnListChanged -= ControllerOnListChanged;
        _controller.GetConfigurationService().Saved -= OnSaved;
        _listItems?.RunDispose();
        base.Dispose();
    }

    private void OnSaved(object? sender, EventArgs e)
    {
        var configuration = _controller.GetConfigurationService().Get();
        _list.SetShowSeparators(configuration.ShowListSeparator);
    }
}