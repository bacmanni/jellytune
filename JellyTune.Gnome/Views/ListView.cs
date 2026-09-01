using GObject;
using Gtk;
using JellyTune.Gnome.Helpers;
using JellyTune.Gnome.Models;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Events;
using ListStore = Gio.ListStore;
using Spinner = Adw.Spinner;

namespace JellyTune.Gnome.Views;

[Subclass<Box>(qualifiedName: "JellyTuneListView")]
[Template<AssemblyResource>("JellyTune.Gnome.Blueprints.list.ui")]
public partial class ListView
{
    private ListController _controller;
    
    [Connect] private Spinner _loader;
    [Connect] private Box _results;
    
    [Connect] private Gtk.ListView _list;
    [Connect] private ScrolledWindow _listWindow;
    private SignalListItemFactory _listFactory;
    
    [Connect] private GridView _grid;
    [Connect] private ScrolledWindow _gridWindow;
    private SignalListItemFactory _gridFactory;
    
    private ListStore _listItems;
    private List<Shared.Models.ListItem> _items = [];

    private bool _initialized;
    
    public static ListView NewWithValues(ListController controller)
    {
        var obj = NewWithProperties([]);
        obj._controller = controller;
        obj.InitializeController();
        return obj;
    }

    private void InitializeController()
    {
        _controller.OnListChanged += ControllerOnListChanged;
        _listItems = ListStore.New(ListRow.GetGType());
        
        //List
        _listFactory = SignalListItemFactory.New();
        _listFactory.OnSetup += ListFactoryOnSetup;
        _listFactory.OnBind += ListFactoryOnBind;
        _listFactory.OnUnbind += ListFactoryOnUnbind;
        
        // Grid
        _gridFactory = SignalListItemFactory.New();
        _gridFactory.OnSetup += GridFactoryOnSetup;
        _gridFactory.OnBind += GridFactoryOnBind;
        _gridFactory.OnUnbind += GridFactoryOnUnbind;
        
        var configuration = _controller.ConfigurationService.Get();
        _list.SetShowSeparators(configuration.ShowListSeparator);
        _controller.ConfigurationService.OnSaved += OnSaved;
        
        var selectionModel = NoSelection.New(_listItems);
        _list.SetFactory(_listFactory);
        _list.SetModel(selectionModel);
        _list.OnActivate += (_, args) =>
        {
            if (_listItems.GetObject(args.Position) is ListRow row)
                _controller.OpenItem(row.Id);
        };
        _list.OnRealize += (_, _) =>
        {
            _list.GrabFocus();
        };
        
        _grid.SetFactory(_gridFactory);
        _grid.SetModel(selectionModel);
        _grid.OnActivate += (_, args) =>
        {
            if (_listItems.GetObject(args.Position) is ListRow row)
                _controller.OpenItem(row.Id);
        };
        _grid.OnRealize += (_, _) =>
        {
            _grid.GrabFocus();
        };

        _initialized = true;
    }

    private void GridFactoryOnUnbind(SignalListItemFactory sender, SignalListItemFactory.UnbindSignalArgs args)
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

    private void ListFactoryOnUnbind(SignalListItemFactory sender, SignalListItemFactory.UnbindSignalArgs args)
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
        if (!_initialized) return;
        
        GtkHelper.GtkDispatch(() =>
        {
            if (args.Items is not null)
            {
                if (args.UpdateOnly)
                {
                    var updateIds = _controller.GetItems().Select(item => item.Id).ToList();
                    var currentIds = _items.Select(item => item.Id).ToList();

                    var addedIds = updateIds.Except(currentIds).ToList();
                    var removedIds = currentIds.Except(updateIds).ToList();

                    if (removedIds.Any())
                    {
                        for (var i = _listItems.GetNItems() - 1; i > 0; i--)
                        {
                            if (_listItems.GetObject(i) is ListRow row &&
                                removedIds.Contains(row.Id))
                                _listItems.Remove(i);
                        }
                    }

                    if (addedIds.Any())
                    {
                        var added = _controller.GetItems().Where(x => addedIds.Contains(x.Id));
                        foreach (var item in added)
                            _listItems.Append(ListRow.New(item));
                    }
                }
                else
                {
                    _listItems.RemoveAll();
                    _items.Clear();
                    foreach (var item in args.Items)
                    {
                        _listItems.Append(ListRow.New(item));
                        _items.Add(item);
                    }
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
        });
    }
    
    private void GridFactoryOnBind(SignalListItemFactory sender, SignalListItemFactory.BindSignalArgs args)
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

    private void GridFactoryOnSetup(SignalListItemFactory sender, SignalListItemFactory.SetupSignalArgs args)
    {
        var listItem = args.Object as Gtk.ListItem;
        if (listItem is null)
        {
            return;
        }
        
        listItem.SetChild(GridItem.NewWithValues(_controller.FileService));
    }

    private void ListFactoryOnBind(SignalListItemFactory sender, SignalListItemFactory.BindSignalArgs args)
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

    private void ListFactoryOnSetup(SignalListItemFactory sender, SignalListItemFactory.SetupSignalArgs args)
    {
        var listItem = args.Object as Gtk.ListItem;
        if (listItem is null)
        {
            return;
        }

        listItem.SetChild(ListItem.NewWithValues(_controller.FileService));
    }

    public override void Dispose()
    {
        _controller.OnListChanged -= ControllerOnListChanged;
        _controller.ConfigurationService.OnSaved -= OnSaved;
        _listItems.RunDispose();
        base.Dispose();
    }

    private void OnSaved(object? sender, EventArgs e)
    {
        var configuration = _controller.ConfigurationService.Get();
        _list.SetShowSeparators(configuration.ShowListSeparator);
    }
}