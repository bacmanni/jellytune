using Adw;
using GLib;
using GObject;
using Gtk;
using JellyTune.Gnome.Models;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using ListStore = Gio.ListStore;
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
    
    private SignalListItemFactory _audioCollectionFactory;
    private ListStore _audioCollectionItems;
    
    private SignalListItemFactory _playlistCollectionFactory;
    private ListStore _playlistCollectionItems;
    
    private Spinner _serverLoading = Spinner.New();
    private Spinner _usernameLoading = Spinner.New();
    private Spinner _passwordLoading = Spinner.New();
    private Spinner _audioCollectionLoading = Spinner.New();
    private Spinner _playlistCollectionLoading = Spinner.New();
    
    private bool _isServerValid;
    private bool _isAccountValid;
    private bool _isCollectionValid;

    public static AccountView NewWithValues(AccountController controller)
    {
        var obj = NewWithProperties([]);
        obj._controller = controller;
        obj.InitializeController();
        return obj;
    }
    
    private void InitializeController()
    {
        _controller.OnConfigurationLoaded += ControllerOnOnConfigurationLoaded;

        _serverLoading.SetVisible(false);
        _server.AddSuffix(_serverLoading);
        _server.OnApply += async (_, _) =>
        {
            await CheckServer();
        };

        _usernameLoading.SetVisible(false);
        _username.AddSuffix(_usernameLoading);
        _username.OnApply += async (_, _) =>
        {
            _usernameLoading.SetVisible(true);
           await CheckLogin();
        };

        _passwordLoading.SetVisible(false);
        _password.AddSuffix(_passwordLoading);
        _password.OnApply += async (_, _) =>
        {
            _passwordLoading.SetVisible(true);
            await CheckLogin();
        };
        
        _audioCollectionItems = ListStore.New(CollectionRow.GetGType());
        var audioSelectionModel = NoSelection.New(_audioCollectionItems);
        _audioCollectionFactory = SignalListItemFactory.New();
        _audioCollectionFactory.OnBind += AudioCollectionFactoryOnBind;
        _audioCollectionFactory.OnSetup += AudioCollectionFactoryOnSetup;
        _audioCollection.SetFactory(_audioCollectionFactory);
        _audioCollection.SetModel(audioSelectionModel);
        _audioCollectionLoading.SetVisible(false);
        _audioCollection.AddSuffix(_audioCollectionLoading);
        
        _playlistCollectionItems = ListStore.New(CollectionRow.GetGType());
        var playlistSelectionModel = NoSelection.New(_playlistCollectionItems);
        _playlistCollectionFactory = SignalListItemFactory.New();
        _playlistCollectionFactory.OnBind += PlaylistCollectionFactoryOnBind;
        _playlistCollectionFactory.OnSetup += PlaylistCollectionFactoryOnSetup;
        _playlistCollection.SetFactory(_playlistCollectionFactory);
        _playlistCollection.SetModel(playlistSelectionModel);
        _playlistCollectionLoading.SetVisible(false);
        _playlistCollection.AddSuffix(_playlistCollectionLoading);
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

    private void ControllerOnOnConfigurationLoaded(object? sender, AccountArgs args)
    {
        _isAccountValid = false;
        _isServerValid = false;
            
        _server.SetText(_controller.ServerUrl ?? string.Empty);
        _username.SetText(_controller.Username ?? string.Empty);
        _password.SetText(_controller.Password != null ? _controller.Password : string.Empty);
        
        if (!args.Validate)
            return;

        _ = Check();
    }

    private async Task Check()
    {
        await CheckServer();
        await CheckLogin();
            
        _controller.UpdateValidity(_isServerValid,  _isAccountValid, _isCollectionValid);
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

    private async Task CheckServer()
    {
        _server.RemoveCssClass("error");
        _username.SetSensitive(false);
        _password.SetSensitive(false);
        _audioCollection.SetSensitive(false);
            
        if (!string.IsNullOrWhiteSpace(_server.GetText()))
        {
            _serverLoading.SetVisible(true);
            var serverUrl = _server.GetText();
            _isServerValid = await _controller.IsValidServerAsync(serverUrl);
            _serverLoading.SetVisible(false);

            if (_isServerValid)
            {
                _controller.ServerUrl = serverUrl;
                _controller.UpdateValidity(true, false, false);
                await CheckLogin();
                _username.SetSensitive(true);
                _password.SetSensitive(true);
            }
            else
            {
                _server.AddCssClass("error");
            }
        }
        else
        {
            _server.AddCssClass("error");
        }
    }
    
    private async Task CheckLogin()
    {
        var username = _username.GetText().Trim();
        var password = _password.GetText().Trim();
        
        if (!_isServerValid)
        {
            _username.RemoveCssClass("error");
            _password.RemoveCssClass("error");
            _usernameLoading.SetVisible(false);
            _passwordLoading.SetSensitive(false);
        }
        
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            _audioCollection.SetSensitive(false);
            _isAccountValid = await _controller.IsValidAccountAsync(username, password);
            _usernameLoading.SetVisible(false);
            _passwordLoading.SetVisible(false);
            
            if (_isAccountValid)
            {
                _controller.Username = username;
                _controller.Password = password;
                _controller.UpdateValidity(true,  true, false);
                _username.RemoveCssClass("error");
                _password.RemoveCssClass("error");
                _audioCollection.SetSensitive(true);
                await UpdateAudioCollections();
                _ = UpdatePlaylistCollections();
            }
            else
            {
                _username.AddCssClass("error");
                _password.AddCssClass("error");
            }
        }
        else
        {
            _usernameLoading.SetVisible(false);
            _passwordLoading.SetVisible(false);
        }
    }

    private async Task UpdateAudioCollections()
    {
        _audioCollection.RemoveCssClass("error");
        _isCollectionValid = false;
        
        if (_isServerValid && _isAccountValid)
        {
            _audioCollectionLoading.SetVisible(true);
            _audioCollectionItems.RemoveAll();
            
            var selectedIndex = -1;
            var collectionId = _controller.GetSelectedAudioCollectionId();
            var collections = await _controller.GetCollectionsAsync(CollectionType.Audio);
            
            for (var index = 0; index < collections.Count; index++)
            {
                var collection = collections[index];
                if (collection.Id == collectionId)
                    selectedIndex = index;
                
                _audioCollectionItems.Append(CollectionRow.New(collection));
            }

            if (selectedIndex != -1)
            {
                _audioCollection.SetSelected(Convert.ToUInt32(selectedIndex));
                _controller.CollectionId = collectionId;
                _controller.UpdateValidity(true, true, true);
                _isCollectionValid = true;
            }
            else if (collections.Count > 0)
            {
                _audioCollection.SetSelected(0);
                var collectionRow = _audioCollection.GetSelectedItem() != null
                    ? _audioCollection.GetSelectedItem() as CollectionRow
                    : null;
                
                _controller.CollectionId = collectionRow?.Id;
                _controller.UpdateValidity(true, true, true);
                _isCollectionValid = true;
            }
            else
            {
                _audioCollection.AddCssClass("error");
                _controller.UpdateValidity(true, true, false);
                _isCollectionValid = false;
            }
            
            _controller.UpdateValidity(_isServerValid,  _isAccountValid, _isCollectionValid);
            _audioCollection.SetSensitive(true);
            _audioCollectionLoading.SetVisible(false);
        }
    }

    private async Task UpdatePlaylistCollections()
    {
        if (_isServerValid && _isAccountValid)
        {
            _playlistCollectionLoading.SetVisible(true);
            _playlistCollection.SetSensitive(false);
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
            else if (collections.Any())
            {
                _playlistCollection.SetSelected(0);
                var playlistCollectionRow = _playlistCollection.GetSelectedItem() != null
                    ? _playlistCollection.GetSelectedItem() as CollectionRow
                    : null;

                _controller.PlaylistCollectionId = playlistCollectionRow?.Id;
            }
            
            _playlistCollectionLoading.SetVisible(false);
            _playlistCollection.SetSensitive(collections.Any());
        }
    }
    
    public override void Dispose()
    {
        _controller.OnConfigurationLoaded -= ControllerOnOnConfigurationLoaded;
        base.Dispose();
    }
}