using Adw.Internal;
using Gtk;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using JellyTune.Shared.Models;
using JellyTune.Gnome.Helpers;
using JellyTune.Gnome.Models;
using SwitchRow = Adw.SwitchRow;

namespace JellyTune.Gnome.Views;

public class AccountView : Adw.PreferencesGroup
{
    private readonly AccountController  _controller;

    [Gtk.Connect] private readonly Adw.EntryRow _server;
    [Gtk.Connect] private readonly Adw.EntryRow _username;
    [Gtk.Connect] private readonly Adw.PasswordEntryRow _password;
    [Gtk.Connect] private readonly Adw.SwitchRow _rememberPassword;
    [Gtk.Connect] private readonly Adw.ComboRow _audioCollection;
    [Gtk.Connect] private readonly Adw.ComboRow _playlistCollection;
    
    private readonly Gtk.SignalListItemFactory _audioCollectionFactory;
    private readonly Gio.ListStore _audioCollectionItems;
    
    private readonly Gtk.SignalListItemFactory _playlistCollectionFactory;
    private readonly Gio.ListStore _playlistCollectionItems;
    
    private Adw.Spinner _serverLoading = Adw.Spinner.New();
    private Adw.Spinner _usernameLoading = Adw.Spinner.New();
    private Adw.Spinner _passwordLoading = Adw.Spinner.New();
    private Adw.Spinner _audioCollectionLoading = Adw.Spinner.New();
    private Adw.Spinner _playlistCollectionLoading = Adw.Spinner.New();
    
    private bool _isServerValid;
    private bool _isAccountValid;
    private bool _isCollectionValid;
    
    private AccountView(Gtk.Builder builder) : base(
        new PreferencesGroupHandle(builder.GetPointer("_root"), false))
    {
        builder.Connect(this);
    }
    
    public AccountView(AccountController controller) : this(Blueprint.BuilderFromFile("account"))
    {
        _controller = controller;
        _controller.OnConfigurationLoaded += ControllerOnOnConfigurationLoaded;

        _serverLoading.SetVisible(false);
        _server.AddSuffix(_serverLoading);
        _server.OnApply += async (sender, args) =>
        {
            await CheckServer();
        };

        _usernameLoading.SetVisible(false);
        _username.AddSuffix(_usernameLoading);
        _username.OnApply += async (sender, args) =>
        {
            _usernameLoading.SetVisible(true);
           await CheckLogin();
        };

        _passwordLoading.SetVisible(false);
        _password.AddSuffix(_passwordLoading);
        _password.OnApply += async (sender, args) =>
        {
            _passwordLoading.SetVisible(true);
            await CheckLogin();
        };

        _rememberPassword.OnNotify += (sender, args) =>
        {
            if (sender is SwitchRow element)
                _controller.RememberPassword = element.GetActive();
        };
        
        _audioCollectionItems = Gio.ListStore.New(CollectionRow.GetGType());
        var audioSelectionModel = Gtk.NoSelection.New(_audioCollectionItems);
        _audioCollectionFactory = Gtk.SignalListItemFactory.New();
        _audioCollectionFactory.OnBind += AudioCollectionFactoryOnBind;
        _audioCollectionFactory.OnSetup += AudioCollectionFactoryOnSetup;
        _audioCollection.SetFactory(_audioCollectionFactory);
        _audioCollection.SetModel(audioSelectionModel);
        _audioCollectionLoading.SetVisible(false);
        _audioCollection.AddSuffix(_audioCollectionLoading);
        
        _playlistCollectionItems = Gio.ListStore.New(CollectionRow.GetGType());
        var playlistSelectionModel = Gtk.NoSelection.New(_playlistCollectionItems);
        _playlistCollectionFactory = Gtk.SignalListItemFactory.New();
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

        var label = Gtk.Label.New(null);
        listItem.SetChild(label);
    }

    private void PlaylistCollectionFactoryOnBind(SignalListItemFactory sender, SignalListItemFactory.BindSignalArgs args)
    {
        var listItem = args.Object as Gtk.ListItem;
        if (listItem is null)
        {
            return;
        }

        var template = listItem.Child as Gtk.Label;
        if (template is null)
        {
            return;
        }

        if (listItem.Item is CollectionRow item)
            template.SetText(GLib.Markup.EscapeText(item.Name));
    }

    private async void ControllerOnOnConfigurationLoaded(object? sender, AccountArgs args)
    {
        _isAccountValid = false;
        _isServerValid = false;
            
        _server.SetText(args.Configuration.ServerUrl);
        _username.SetText(args.Configuration.Username);
        _password.SetText(args.Configuration.Password);
        _rememberPassword.SetActive(args.Configuration.RememberPassword);

        if (!args.Validate)
            return;
        
        await CheckServer();
        await CheckLogin();
            
        _controller.UpdateValidity(_isServerValid,  _isAccountValid, _isCollectionValid);
    }

    private void AudioCollectionFactoryOnSetup(Gtk.SignalListItemFactory sender, Gtk.SignalListItemFactory.SetupSignalArgs args)
    {
        var listItem = args.Object as Gtk.ListItem;
        if (listItem is null)
        {
            return;
        }

        var label = Gtk.Label.New(null);
        listItem.SetChild(label);
    }

    private void AudioCollectionFactoryOnBind(Gtk.SignalListItemFactory sender, Gtk.SignalListItemFactory.BindSignalArgs args)
    {
        var listItem = args.Object as Gtk.ListItem;
        if (listItem is null)
        {
            return;
        }

        var template = listItem.Child as Gtk.Label;
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
            _isServerValid = await _controller.IsValidServer(serverUrl);
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
            _isAccountValid = await _controller.IsValidAccount(username, password);
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
                UpdatePlaylistCollections();
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
            var collections = await _controller.GetCollections(CollectionType.Audio);
            
            for (var index = 0; index < collections.Count; index++)
            {
                var collection = collections[index];
                if (collection.Id == collectionId)
                    selectedIndex = index;
                
                _audioCollectionItems.Append(new CollectionRow(collection));
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
                _controller.CollectionId = (_audioCollection.GetSelectedItem() as CollectionRow)?.Id;
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
            var collections = await _controller.GetCollections(CollectionType.Playlist);
            for (var index = 0; index < collections.Count; index++)
            {
                var collection = collections[index];
                if (collection.Id == collectionId)
                    selectedIndex = index;
                
                _playlistCollectionItems.Append(new CollectionRow(collection));
            }
            
            if (selectedIndex != -1)
            {
                _playlistCollection.SetSelected(Convert.ToUInt32(selectedIndex));
                _controller.PlaylistCollectionId = collectionId;
            }
            else if (collections.Any())
            {
                _playlistCollection.SetSelected(0);
                _controller.PlaylistCollectionId = (_playlistCollection.GetSelectedItem() as CollectionRow)?.Id;
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