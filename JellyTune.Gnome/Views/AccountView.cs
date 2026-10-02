using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Adw;
using GLib;
using GObject;
using Gtk;
using JellyTune.Gnome.Models;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using ListStore = Gio.ListStore;
using Object = GObject.Object;
using Spinner = Adw.Spinner;

namespace JellyTune.Gnome.Views;

[Subclass<PreferencesGroup>(qualifiedName: "JellyTuneAccountView")]
[Template<AssemblyResource>("JellyTune.Gnome.Blueprints.account.ui")]
public partial class AccountView
{
    private AccountController  _controller;

    [Connect] private EntryRow _server;
    [Connect] private EntryRow _username;
    [Connect] private PasswordEntryRow _password;
    [Connect] private ComboRow _audioCollection;
    [Connect] private ComboRow _playlistCollection;

    [Connect] private Revealer _updateRevealer;
    [Connect] private Button _updateButton;
    [Connect] private Stack _updateStack;
    [Connect] private Label _updateLabel;
    [Connect] private Adw.Spinner _updateSpinner;
    
    [Connect] private Popover _errorPopover;
    [Connect] private Label _errorPopoverLabel;
    
    private SignalListItemFactory _audioCollectionFactory;
    private ListStore _audioCollectionItems;
    
    private SignalListItemFactory _playlistCollectionFactory;
    private ListStore _playlistCollectionItems;

    private CancellationTokenSource? _checkAccountCts;
    private bool _showUpdateButton { get; set; }
    private bool _loading = true;
    
    
    public static AccountView NewWithValues(AccountController controller, bool showUpdateButton)
    {
        var obj = NewWithProperties([]);
        obj._controller = controller;
        obj._showUpdateButton = showUpdateButton;
        obj.InitializeController();
        return obj;
    }
    
    private void InitializeController()
    {
        _controller.OnConfigurationLoaded += ControllerOnConfigurationLoaded;
        _controller.OnConfigurationValueChanged += ControllerOnConfigurationValueChanged;
        _updateButton.OnClicked += UpdateButtonOnClicked;

        _server.OnChanged += (sender, args) =>
        {
            if (sender.GetText() == _controller.ServerUrl) return;
            
            ResetCollections();
            _controller.ServerUrl = sender.GetText();
            _controller.ConfigurationValueChanged();
        };

        _username.OnChanged += (sender, args) =>
        {
            if (sender.GetText() == _controller.Username) return;
            
            ResetCollections();
            _controller.Username = sender.GetText();
            _controller.ConfigurationValueChanged();
        };

        _password.OnChanged += (sender, args) =>
        {
            if (sender.GetText() == _controller.Password) return;
            
            ResetCollections();
            _controller.Password = sender.GetText();
            _controller.ConfigurationValueChanged();
        };

        _audioCollectionItems = ListStore.New(CollectionRow.GetGType());
        var audioSelectionModel = NoSelection.New(_audioCollectionItems);
        _audioCollectionFactory = SignalListItemFactory.New();
        _audioCollectionFactory.OnBind += AudioCollectionFactoryOnBind;
        _audioCollectionFactory.OnSetup += AudioCollectionFactoryOnSetup;
        _audioCollection.SetFactory(_audioCollectionFactory);
        _audioCollection.SetModel(audioSelectionModel);
        _audioCollection.OnNotify += AudioCollectionOnNotify;
        
        _playlistCollectionItems = ListStore.New(CollectionRow.GetGType());
        var playlistSelectionModel = NoSelection.New(_playlistCollectionItems);
        _playlistCollectionFactory = SignalListItemFactory.New();
        _playlistCollectionFactory.OnBind += PlaylistCollectionFactoryOnBind;
        _playlistCollectionFactory.OnSetup += PlaylistCollectionFactoryOnSetup;
        _playlistCollection.SetFactory(_playlistCollectionFactory);
        _playlistCollection.SetModel(playlistSelectionModel);
        _playlistCollection.OnNotify += PlaylistCollectionOnNotify;
    }

    private void ControllerOnConfigurationValueChanged(object? sender, EventArgs e)
    {
        if (_showUpdateButton)
            _updateRevealer.SetRevealChild(true);
    }

    private void PlaylistCollectionOnNotify(Object sender, NotifySignalArgs args)
    {
        if (args.Pspec.GetName() != "selected") return;
        var selected = _playlistCollection.GetSelectedItem() as CollectionRow;
        if (selected?.Id != _controller.PlaylistCollectionId)
        {
            _controller.ConfigurationValueChanged();
        }
    }

    private void AudioCollectionOnNotify(Object sender, NotifySignalArgs args)
    {
        if (args.Pspec.GetName() != "selected") return;
        var selected = _audioCollection.GetSelectedItem() as CollectionRow;
        if (selected?.Id != _controller.CollectionId)
        {
            _controller.ConfigurationValueChanged();
        }
    }

    private async void UpdateButtonOnClicked(Button sender, EventArgs args)
    {
        try
        {
            _updateButton.SetSensitive(false);
            _updateStack.SetVisibleChild(_updateSpinner);
            var success = await Check();
            if (success)
            {
                _updateRevealer.SetRevealChild(false);
            }
            else
            {
                _updateStack.SetVisibleChild(_updateLabel);
                _updateButton.SetSensitive(true);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"{e.Message}");
            Console.WriteLine($"{e.StackTrace}");
        }
    }

    private void AddErrorPopup(List<Widget> parents, string text)
    {
        if (parents.Count == 0) throw new Exception("Parameter parents requires at least one item");

        foreach (var parent in parents)
        {
            parent.AddCssClass("error");
        }
        
        _errorPopoverLabel.SetText(text);
        _errorPopover.Unparent();
        _errorPopover.SetParent(parents[0]);
        _errorPopover.Popup();
    }
    
    private void PlaylistCollectionFactoryOnSetup(SignalListItemFactory sender, SignalListItemFactory.SetupSignalArgs args)
    {
        var listItem = args.Object as Gtk.ListItem;
        if (listItem is null)
        {
            return;
        }

        var label = Label.New(null);
        listItem.SetChild(label);
    }

    private void PlaylistCollectionFactoryOnBind(SignalListItemFactory sender, SignalListItemFactory.BindSignalArgs args)
    {
        var listItem = args.Object as Gtk.ListItem;
        if (listItem is null)
        {
            return;
        }

        var template = listItem.Child as Label;
        if (template is null)
        {
            return;
        }

        if (listItem.Item is CollectionRow item)
            template.SetText(Markup.EscapeText(item.Name));
    }

    private void ControllerOnConfigurationLoaded(object? sender, AccountArgs args)
    {
        _server.SetText(_controller.ServerUrl ?? string.Empty);
        _username.SetText(_controller.Username ?? string.Empty);
        _password.SetText(_controller.Password ?? string.Empty);
        _loading = false;
    }

    private void ResetCollections()
    {
        if (_loading) return;
        
        _audioCollection.SetSensitive(false);
        _audioCollection.RemoveCssClass("error");
        _audioCollectionItems.RemoveAll();
        
        _playlistCollection.SetSensitive(false);
        _playlistCollection.RemoveCssClass("error");
        _playlistCollectionItems.RemoveAll();
    }
    
    public async Task<bool> Check()
    {
        _server.RemoveCssClass("error");
        _username.RemoveCssClass("error");
        _password.RemoveCssClass("error");
        _audioCollection.RemoveCssClass("error");
        _playlistCollection.RemoveCssClass("error");
        
        SetFormSensitive(false);
        _errorPopover.Popdown();
        _controller.SetValid(false);

        var validServer = await CheckServer();
        if (!validServer)
        {
            SetFormSensitive(true);
            return false;
        }
    
        var validAccount = await CheckLogin();
        if (!validAccount)
        {
            SetFormSensitive(true);
            return false;
        }
        
        var validCollection = await UpdateCollections();
        _controller.SetValid(validCollection);
        SetFormSensitive(true);
        return validCollection;
    }

    private void SetFormSensitive(bool sensitive)
    {
        _server.SetSensitive(sensitive);
        _username.SetSensitive(sensitive);
        _password.SetSensitive(sensitive);
    }
    
    private void AudioCollectionFactoryOnSetup(SignalListItemFactory sender, SignalListItemFactory.SetupSignalArgs args)
    {
        var listItem = args.Object as Gtk.ListItem;
        if (listItem is null)
        {
            return;
        }

        var label = Label.New(null);
        listItem.SetChild(label);
    }

    private void AudioCollectionFactoryOnBind(SignalListItemFactory sender, SignalListItemFactory.BindSignalArgs args)
    {
        var listItem = args.Object as Gtk.ListItem;
        if (listItem is null)
        {
            return;
        }

        var template = listItem.Child as Label;
        if (template is null)
        {
            return;
        }

        if (listItem.Item is CollectionRow item)
            template.SetText(item.Name);
    }

    private async Task<bool> CheckServer()
    {
        var serverUrl = _server.GetText();

        if (!_controller.IsValidServerUrl(serverUrl))
        {
            AddErrorPopup([_server], "Invalid server url");
            return false;
        }
        
        var isValid = await _controller.IsValidServerAsync(serverUrl);
        if (!isValid)
        {
            AddErrorPopup([_server], $"Invalid Jellyfin server");
            return false;
        }
        
        _controller.ServerUrl = serverUrl;
        return true;
    }
    
    private async Task<bool> CheckLogin()
    {
        var username = _username.GetText().Trim();
        var password = _password.GetText().Trim();
        _username.RemoveCssClass("error");
        _password.RemoveCssClass("error");
        
        var isValid = await _controller.IsValidAccountAsync(username, password);
        if (!isValid)
        {
            AddErrorPopup([_password, _username], "Invalid username or password");
            return false;
        }

        _controller.Username = username;
        _controller.Password = password;
        return true;
    }

    public async Task<bool> UpdateCollections()
    {
        _audioCollection.RemoveCssClass("error");
        _audioCollectionItems.RemoveAll();

        var selectedIndex = -1;
        var collectionId = _controller.GetSelectedAudioCollectionId();
        var collections = await _controller.GetCollectionsAsync(CollectionType.Audio);
        
        // Update also playlists
        var selectedPlaylist = await UpdatePlaylistCollections();
        
        if (collections.Count == 0)
        {
            AddErrorPopup([_audioCollection], "No audio collections found. At least one audio collection is required");
            return false;
        }
        
        for (var index = 0; index < collections.Count; index++)
        {
            var collection = collections[index];
            if (collection.Id == collectionId)
                selectedIndex = index;
            
            _audioCollectionItems.Append(CollectionRow.New(collection));
        }
        
        // Only one collection found. Select it automatically
        if (selectedIndex != -1)
        {
            _audioCollection.SetSelected(Convert.ToUInt32(selectedIndex));
            _controller.CollectionId = collectionId;
        }
        // Nothing is selected or this is startup
        else if (collectionId == null && !selectedPlaylist)
        {
            // Preselect all that have only one collection
            if (_playlistCollectionItems.NItems == 1)
            {
                if (_playlistCollectionItems.GetObject(0) is CollectionRow playlist)
                {
                    _playlistCollection.SetSelected(0);
                    _controller.PlaylistCollectionId = playlist.Id;
                }
            }
            
            if (_audioCollectionItems.NItems == 1)
            {
                _audioCollection.SetSelected(0);
                _controller.CollectionId = collections[0].Id;
            }
            else if (_audioCollectionItems.NItems > 1)
            {
                AddErrorPopup([_playlistCollection, _audioCollection], "Select audio and playlist collection");
                return false;   
            }
        }
        
        _audioCollection.SetSensitive(collections.Count > 0);
        var collectionRow = _audioCollection.GetSelectedItem() != null
            ? _audioCollection.GetSelectedItem() as CollectionRow
            : null;
            
        _controller.CollectionId = collectionRow?.Id;
        return true;
    }

    private async Task<bool> UpdatePlaylistCollections()
    {
            _playlistCollectionItems.RemoveAll();
            
            var selectedIndex = -1;
            var collectionId = _controller.GetSelectedPlaylistCollectionId();
            var collections = await _controller.GetCollectionsAsync(CollectionType.Playlist);

            for (var index = 0; index < collections.Count; index++)
            {
                var collection = collections[index];
                if (collection.Id == collectionId)
                    selectedIndex = index;
                
                _playlistCollectionItems.Append(CollectionRow.New(collection));
            }

            if (selectedIndex != -1)
            {
                _playlistCollection.SetSelected(Convert.ToUInt32(selectedIndex));
                _controller.PlaylistCollectionId = collectionId;
            }

            _playlistCollection.SetSensitive(collections.Count > 0);
            return collectionId.HasValue;
    }
    
    public override void Dispose()
    {
        _controller.OnConfigurationLoaded -= ControllerOnConfigurationLoaded;
        base.Dispose();
    }
}