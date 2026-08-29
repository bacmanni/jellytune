using System.Reflection;
using Adw;
using Gio;
using GLib;
using GObject;
using Gtk;
using JellyTune.Gnome.DBus.MediaPlayer;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using AboutDialog = Adw.AboutDialog;
using Application = Adw.Application;
using ApplicationWindow = Adw.ApplicationWindow;
using Dialog = Adw.Dialog;
using HeaderBar = Adw.HeaderBar;
using Object = GObject.Object;
using ShortcutsSection = Adw.ShortcutsSection;
using Spinner = Adw.Spinner;
using Task = System.Threading.Tasks.Task;

namespace JellyTune.Gnome.Views;

/// <summary>
/// The MainWindow for the application
/// </summary>
[Subclass<ApplicationWindow>(qualifiedName: "JellyTuneMainWindow")]
[Template<AssemblyResource>("JellyTune.Gnome.Blueprints.window.ui")]
public partial class MainWindow
{
    private MainWindowController _controller;
    private Application _application;
    
    private StartupController _startupController;

    private MediaPlayerService _mediaPlayerService;

    private PlayerController _playerController;
    private PlayerView  _playerView;

    private PlayerExtendedController _playerExtendedController;
    private PlayerExtendedView _playerExtendedView;

    private ArtistAlbumController _artistAlbumController;

    private AlbumController _albumController;
    private AlbumView _albumView;
    
    private AlbumlistController _albumlistController;
    private AlbumListView _albumListView;
    
    private SearchController _searchController;
    private SearchView _searchView;
    
    private QueueListController _queueListController;
    private QueueListView _queueListView;
    
    private PlaylistController _playlistController;
    private PlaylistView _playlistView;

    private PlaylistTracksController _playlistTracksController;
    private PlaylistTracksView _playlistTracksView;

    private int _breakpoint = 500;
    
    private SimpleAction _refreshAction;
    private SimpleAction _viewAction;
    
    private CancellationTokenSource? _menuUpdateCancellationTokenSource;
    private CancellationTokenSource? _searchAlbumsCts;
    
    [Connect] private Button _searchButton;
    [Connect] private SearchEntry _searchField;
    
    [Connect] private Box _playerPosition;
    
    [Connect] private Box _player;
    [Connect] private Revealer _playerRevealer;
    
    [Connect] private MenuButton _menuButton;
    [Connect] private Spinner _spinner;
    
    [Connect] private ViewSwitcher _switcherTitle;
    [Connect] private NavigationPage _mainView;
    [Connect] private NavigationPage _albumDetails;
    [Connect] private NavigationPage _searchAlbums;
    [Connect] private NavigationPage _queueList;
    [Connect] private NavigationPage _playlistTracks;
    
    [Connect] private NavigationView _rootView;
    [Connect] private ToolbarView _mainToolbarView;
    [Connect] private ToolbarView _albumDetailsToolbarView;
    [Connect] private ToolbarView _searchToolbarView;
    [Connect] private ToolbarView _queueListToolbarView;
    [Connect] private ToolbarView _playlistTracksToolbarView;
    
    [Connect] private HeaderBar _mainHeaderBar;
    [Connect] private ViewStack _mainViewStack;
    
    [Connect] private Box _mainStackAlbums;
    [Connect] private Box _mainStackPlaylists;
    
    // This is stupid hack. Used for displaying shadow correctly on player
    [Connect] private Box _mainFooter;
    [Connect] private Box _albumDetailsFooter;
    [Connect] private Box _searchFooter;
    [Connect] private Box _queueListFooter;
    [Connect] private Box _playlistTracksFooter;

    // Navigation view buttons
    [Connect] private Button _artistAlbumsButton;
    [Connect] private Button _queueListShuffleButton;
    [Connect] private Button _queueListAlbumButton;
    [Connect] private Button _queueListArtistAlbumsButton;

    private bool _initialized;
    
    public static MainWindow NewWithValues(MainWindowController controller, Application application)
    {
        var obj =  NewWithProperties([]);
        obj._controller = controller;
        obj._application = application;
        obj.InitializeController();
        return obj;
    }

    private void InitializeController()
    {
        _controller.PlayerService.OnPlayerStateChanged += OnPlayerStateChanged;

        // Album list
        _albumlistController = new AlbumlistController(_controller.JellyTuneApiService,
            _controller.ConfigurationService, _controller.FileService);
        _albumlistController.OnAlbumClicked += AlbumListControllerOnAlbumClicked;
        _albumListView = AlbumListView.NewWithValues(_albumlistController);
        
        //Album details
        _albumController = new AlbumController(_controller.JellyTuneApiService, _controller.ConfigurationService, _controller.PlayerService, _controller.FileService);
        _albumView = AlbumView.NewWithValues(_albumController);

        _albumController.OnAlbumChanged += AlbumControllerOnAlbumChanged;
        
        // Startup
        _startupController = new StartupController(_controller.JellyTuneApiService, _controller.ConfigurationService);

        // Media controls
        _mediaPlayerService = new MediaPlayerService(this, _controller.FileService, _controller.PlayerService, _controller.ApplicationInfo);

        //Audio player
        _playerExtendedController = new PlayerExtendedController(_controller.PlayerService, _controller.ConfigurationService);
        
        _playerController = new PlayerController(_controller.JellyTuneApiService, _controller.ConfigurationService, _controller.PlayerService);
        _playerController.OnShowPlaylistClicked += PlayerControllerOnShowPlaylistClicked;
        _playerController.OnShowShowLyricsClicked += PlayerControllerOnShowShowLyricsClicked;
        _playerView = PlayerView.NewWithValues(_playerController, _playerExtendedController);

        _playerExtendedView = PlayerExtendedView.NewWithValues(_playerExtendedController);

        _artistAlbumController = new ArtistAlbumController(_controller.JellyTuneApiService, _controller.FileService);
        
        // Search
        _searchController = new SearchController(_controller.JellyTuneApiService, _controller.FileService);
        _searchController.OnAlbumClicked += SearchControllerOnAlbumClicked;
        _searchView = SearchView.NewWithValues(_searchController);
        
        // Queue list for currently playling queue
        _queueListController = new QueueListController(_controller.PlayerService, _controller.FileService);
        _queueListView = QueueListView.NewWithValues(_queueListController);
        
        // Playlist
        _playlistController = new PlaylistController(_controller.JellyTuneApiService, _controller.ConfigurationService, _controller.FileService);
        _playlistView = PlaylistView.NewWithValues(_playlistController);
        _playlistController.OnPlaylistClicked += PlaylistControllerOnPlaylistClicked;
        
        _playlistTracksController = new PlaylistTracksController(_controller.JellyTuneApiService, _controller.PlayerService, _controller.FileService);
        _playlistTracksView = PlaylistTracksView.NewWithValues(_playlistTracksController);

        //Refresh application
        _refreshAction = SimpleAction.New("refresh", null);
        _refreshAction.OnActivate += ActRefreshOnActivate;
        AddAction(_refreshAction);
        _application.SetAccelsForAction("win.refresh", new[] { "<Ctrl>F5" });

        //Preferences Action
        var actPreferences = SimpleAction.New("preferences", null);
        actPreferences.OnActivate += ActPreferencesOnActivate;
        AddAction(actPreferences);
        _application.SetAccelsForAction("win.preferences", new[] { "<Ctrl>comma" });
        
        //About Action
        var actAbout = SimpleAction.New("about", null);
        actAbout.OnActivate += ActAboutOnOnActivate;
        AddAction(actAbout);

        var actShortcuts = SimpleAction.New("shortcuts", null);
        actShortcuts.OnActivate += ActShortcutOnActivate;
        AddAction(actShortcuts);
        _application.SetAccelsForAction("win.shortcuts", new[] { "<Ctrl>question" });
        
        //Search
        var actSearchBar = SimpleAction.New("search", null);
        actSearchBar.OnActivate += ActShowSearchBarOnOnActivate;
        AddAction(actSearchBar);
        _application.SetAccelsForAction("win.search", new[] { "<Ctrl>f" });

        // Basic controls
        var actNextTrack = SimpleAction.New("track_next", null);
        actNextTrack.OnActivate += ActNextTrackOnActivate;
        AddAction(actNextTrack);
        _application.SetAccelsForAction("win.track_next", new[] { "<Ctrl>Right" });

        var actPrevious = SimpleAction.New("track_previous", null);
        actPrevious.OnActivate += ActPreviousOnActivate;
        AddAction(actPrevious);
        _application.SetAccelsForAction("win.track_previous", new[] { "<Ctrl>Left" });
        
        var actPlayPause = SimpleAction.New("track_play", null);
        actPlayPause.OnActivate += ActPlayPauseOnActivate;
        AddAction(actPlayPause);
        _application.SetAccelsForAction("win.track_play", new[] { "<Ctrl>space" });

        var actVolumeUp = SimpleAction.New("volume_up", null);
        actVolumeUp.OnActivate += ActVolumeUpOnActivate;
        AddAction(actVolumeUp);
        _application.SetAccelsForAction("win.volume_up", new[] { "<Ctrl>Up" });
        
        var actVolumeDown = SimpleAction.New("volume_down", null);
        actVolumeDown.OnActivate += ActVolumeDownOnActivate;
        AddAction(actVolumeDown);
        _application.SetAccelsForAction("win.volume_down", new[] { "<Ctrl>Down" });
        
        // Lyrics
        var actLyrics = SimpleAction.New("track_lyrics", null);
        actLyrics.OnActivate += ActLyricsOnActivate;
        AddAction(actLyrics);
        _application.SetAccelsForAction("win.track_lyrics", new[] { "<Ctrl>l" });

        var actOpenAlbum = SimpleAction.New("open_album", VariantType.String);
        actOpenAlbum.OnActivate += ActOpenAlbumOnActivate;
        AddAction(actOpenAlbum);

        //Quit Action
        var actQuit = SimpleAction.New("quit", null);
        actQuit.OnActivate += Quit;
        AddAction(actQuit);
        _application.SetAccelsForAction("win.quit", new[] { "<Ctrl>q" });

        // Event for selected view
        _viewAction = SimpleAction.NewStateful(
            "view",
            VariantType.String,
            Variant.NewString("page1")
        );
        
        _viewAction.OnChangeState += (_, args) =>
        {
            if (args.Value == null)
                return;
                
            _viewAction.SetState(args.Value);
            var newState = args.Value.Print(false).Trim('\'');
            
            if (_mainViewStack.VisibleChildName != newState)
                _mainViewStack.SetVisibleChildName(newState);
        };
        AddAction(_viewAction);
        _application.SetAccelsForAction("win.view('page1')", new[] { "<Ctrl>1" });
        _application.SetAccelsForAction("win.view('page2')", new[] { "<Ctrl>2" });
        
        SetIconName(_controller.ApplicationInfo.Icon);
        SetWindowSize(360, 600);
        
        _mainViewStack.OnNotify += (_, args) =>
        {
            if (args.Pspec.GetName() == "visible-child")
            {
                if (_mainViewStack.VisibleChildName != null) 
                    _viewAction.ChangeState(Variant.NewString(_mainViewStack.VisibleChildName));
            }
        };

        _artistAlbumsButton.OnClicked += ShowArtistAlbumsButtonOnClicked;
        _queueListShuffleButton.OnClicked += QueueListShuffleButtonOnClicked;
        _queueListAlbumButton.OnClicked += QueueListAlbumButtonOnClicked;
        _queueListArtistAlbumsButton.OnClicked += QueueListArtistAlbumsButtonOnClicked;
        _spinner.SetVisible(false);
        _rootView.SetVisible(true);
        
        _mainStackAlbums.Append(_albumListView);
        _albumDetailsToolbarView.SetContent(_albumView);
        _player.Append(_playerView);
        _playerPosition.Append(_playerExtendedView);
        _searchToolbarView.SetContent(_searchView);
        _searchField.OnSearchChanged += SearchFieldOnSearchChanged;
        _queueListToolbarView.SetContent(_queueListView);
        _mainStackPlaylists.Append(_playlistView);
        _playlistTracksToolbarView.SetContent(_playlistTracksView);
        OnNotify += OnOnNotify;
        _initialized = true;
    }

    private void QueueListArtistAlbumsButtonOnClicked(Button sender, EventArgs args)
    {
        var trackId = _controller.PlayerService.GetSelectedTrack() != null ? _controller.PlayerService.GetSelectedTrack()?.Id : null;
        if (!trackId.HasValue) return;
        
        var artistAlbumView = ArtistAlbumView.NewWithValues(_artistAlbumController);
        _ = _artistAlbumController.OpenByTrackIdAsync(trackId.Value);
        artistAlbumView.Present(this);
        artistAlbumView.OnClosed += ArtistAlbumViewOnClosed;
    }

    private void QueueListAlbumButtonOnClicked(Button sender, EventArgs args)
    {
        var albumId = _playerController.PlayerService.GetSelectedAlbum() != null ? _playerController.PlayerService.GetSelectedAlbum()?.Id : null;
        if (albumId == null) return;

        _ = _albumController.OpenAsync(albumId.Value);
        _rootView.Push(_albumDetails);
    }

    private void AlbumControllerOnAlbumChanged(object? sender, AlbumStateArgs e)
    {
        if (e is { UpdateAlbum: false, UpdateTracks: false, UpdateTrackState: false, UpdateArtwork: false })
        {
            _artistAlbumsButton.SetSensitive(false);
        }
        else if (e.UpdateAlbum)
        {
            _artistAlbumsButton.SetSensitive(true);
        }
    }

    private void QueueListShuffleButtonOnClicked(Button sender, EventArgs args)
    {
        _queueListController.ShuffleTracks();
    }

    private void ShowArtistAlbumsButtonOnClicked(Button sender, EventArgs args)
    {
        var artistId = _albumController.Album?.ArtistId;
        if (artistId == null) return;
        
        var artistAlbumView = ArtistAlbumView.NewWithValues(_artistAlbumController);
        _ = _artistAlbumController.OpenByArtistIdAsync(artistId.Value);
        artistAlbumView.Present(this);
        artistAlbumView.OnClosed += ArtistAlbumViewOnClosed;
    }

    private void ActShortcutOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var shortcuts = ShortcutsDialog.New();
        
        var general = ShortcutsSection.New("General");
        general.Add(CreateShortcutsItem("Show Search", "win.search"));
        general.Add(CreateShortcutsItem("Show Albums", "win.view('page1')"));
        general.Add(CreateShortcutsItem("Show Playlists", "win.view('page2')"));
        general.Add(CreateShortcutsItem("Refresh Albums From Server", "win.refresh"));
        general.Add(CreateShortcutsItem("Show Preferences", "win.preferences"));
        general.Add(CreateShortcutsItem("Quit", "win.quit"));
        shortcuts.Add(general);
        
        var player = ShortcutsSection.New("Player");
        player.Add(CreateShortcutsItem("Play/Pause", "win.track_play"));
        player.Add(CreateShortcutsItem("Next Track", "win.track_next"));
        player.Add(CreateShortcutsItem("Previous Track", "win.track_previous"));
        player.Add(CreateShortcutsItem("Volume Up", "win.volume_up"));
        player.Add(CreateShortcutsItem("Volume Down", "win.volume_down"));
        player.Add(CreateShortcutsItem("Show Playing Track Lyrics", "win.track_lyrics"));
        shortcuts.Add(player);
        
        shortcuts.Present(this);
        shortcuts.OnClosed += ShortcutsWindowOnClosed;
    }

    private ShortcutsItem CreateShortcutsItem(string title, string acceleratorName)
    {
        var accelerators = _application.GetAccelsForAction(acceleratorName);

        if (accelerators.Length != 1)
            throw new Exception("Missing or too many accelerators");
            
        var item = ShortcutsItem.New(title, accelerators[0]);
        return item;
    }
    
    private void ShortcutsWindowOnClosed(Dialog sender, EventArgs args)
    {
        sender.OnClosed -= ShortcutsWindowOnClosed;
        sender.Dispose();
    }

    private void ActOpenAlbumOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        if (args.Parameter == null) return;
        
        var albumIdParameter = args.Parameter.GetString(out _);
        var albumId = Guid.Parse(albumIdParameter);
        
        var visibleTag = _rootView.VisiblePageTag;
        if ((_albumController.Album != null && albumId == _albumController.Album.Id) && visibleTag == "album_details") return;
        
        ResetNavigationView();
        _ = _albumController.OpenAsync(albumId);
        _rootView.Push(_albumDetails);
    }

    private void ArtistAlbumViewOnClosed(Dialog sender, EventArgs args)
    {
        sender.OnClosed -= ArtistAlbumViewOnClosed;
        sender.Dispose();
    }

    private void ActVolumeDownOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var previous = _controller.PlayerService.GetVolumePercent() - 15;
        
        if (previous <= 0)
            previous = 0;
        
        _controller.PlayerService.SetVolumePercent(previous);
    }

    private void ActVolumeUpOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var next = _controller.PlayerService.GetVolumePercent() + 15;
        if (next > 100)
            next = 100;
        
        _controller.PlayerService.SetVolumePercent(next);
    }

    private void ActLyricsOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        _playerController.ShowShowLyrics();
    }

    private void ActPlayPauseOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        _controller.PlayerService.StartOrPauseTrackAsync();
    }

    private void ActPreviousOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        _controller.PlayerService.PreviousTrackAsync();
    }

    private void ActNextTrackOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        _controller.PlayerService.NextTrackAsync();
    }

    private void PlaylistControllerOnPlaylistClicked(object? sender, Guid id)
    {
        ResetNavigationView();
        _ = _playlistTracksController.OpenPlaylist(id);
        _rootView.Push(_playlistTracks);
    }

    private void OnOnNotify(Object sender, NotifySignalArgs args)
    {
        var name = args.Pspec.GetName();
        if (name != "default-width" && name != "maximized") return;
        _ = UpdateMainMenu(name == "maximized");
    }

    private async Task RefreshLists(bool reload = false)
    {
        _refreshAction.SetEnabled(false);
        await _albumlistController.Refresh(reload);
        await _playlistController.RefreshAsync(reload);
        _refreshAction.SetEnabled(true);
    }
    
    private async Task UpdateMainMenu(bool delay = false)
    {
        if (!_initialized) return;
        if (!_controller.HasMultipleCollections()) return;
        
        _menuUpdateCancellationTokenSource?.Cancel();
        _menuUpdateCancellationTokenSource = new CancellationTokenSource();
        var width1 = GetScreenSize().Item1;

        if (delay)
        {
            int? width2;
            
            do
            {
                width1 = GetAllocatedWidth();
                await Task.Delay(50, _menuUpdateCancellationTokenSource.Token);
                width2 = GetAllocatedWidth();
            } while (width1 != width2);
            
            if (_menuUpdateCancellationTokenSource.IsCancellationRequested) return;
        }

        var show = width1 < _breakpoint;
        var mainMenu = _menuButton.MenuModel as Menu;
        if (mainMenu == null) throw new Exception("Main menu not found");
        
        var existingSection = mainMenu.GetItemLink(0, "section") as Menu;
        if (existingSection == null) throw new Exception("Section not found");
        
        var hasSection = existingSection.GetItemAttributeValue(0, "action", VariantType.String)?.Print(false)
            .Trim('\'').Contains("win.view");
        if (show)
        {
            _mainHeaderBar.TitleWidget?.SetVisible(false);
            
            if (hasSection is false or null)
            {
                var section = Menu.New();
                section.Insert(0, "Music", "win.view('page1')");
                section.Insert(1, "Playlist", "win.view('page2')");
                mainMenu.InsertSection(0, null, section);
            }
        }
        else
        {
            _mainHeaderBar.TitleWidget?.SetVisible(true);

            if (hasSection is true)
            {
                mainMenu.Remove(0);
            }
        }
    }

    private void OnPlayerStateChanged(object? sender, PlayerStateArgs args)
    {
        if (args.State is PlayerState.Playing or PlayerState.Stopped or PlayerState.Paused)
        {
            if (!_mainFooter.IsVisible())
                _mainFooter.SetVisible(true);
                
            if (!_albumDetailsFooter.IsVisible())
                _albumDetailsFooter.SetVisible(true);
                
            if (!_playerRevealer.GetChildRevealed())
                _playerRevealer.SetRevealChild(true);
                
            if (!_playerPosition.IsVisible())
                _playerPosition.SetVisible(true);
                
            if (!_searchFooter.IsVisible())
                _searchFooter.SetVisible(true);
                
            if (!_queueListFooter.IsVisible())
                _queueListFooter.SetVisible(true);

            if (!_playlistTracksFooter.IsVisible())
                _playlistTracksFooter.SetVisible(true);

            UpdateHeader(true);
        }
        else if (args.State is PlayerState.None)
        {
            _playerRevealer.SetRevealChild(false);
            _mainFooter.SetVisible(false);
            _albumDetailsFooter.SetVisible(true);
            _searchFooter.SetVisible(true);
            _queueListFooter.SetVisible(false);
            _playlistTracksFooter.SetVisible(false);
            _playerPosition.SetVisible(false);

            UpdateHeader(false);
        }
    }

    private void UpdateHeader(bool visible)
    {
        _queueListAlbumButton.SetSensitive(visible);
        _queueListArtistAlbumsButton.SetSensitive(visible);
    }
    
    private void SearchFieldOnSearchChanged(SearchEntry sender, EventArgs args)
    {
        _searchAlbumsCts?.Cancel();
        _searchAlbumsCts?.Dispose();
        
        _searchAlbumsCts = new CancellationTokenSource();
        var cancellationToken = _searchAlbumsCts.Token;

        var value = sender.GetText();
        
        if (string.IsNullOrEmpty(value))
        {
            _searchController.StartSearch();
        }
        else
        {
            _ = _searchController.SearchAlbumsAsync(value, cancellationToken);
        }
    }
    
    public (int, int) GetScreenSize()
    {
        return (DefaultWidth, DefaultHeight);
    }

    private void SetWindowSize(int width, int height)
    {
        var savedSize = _controller.GetWindowSize();
        if (savedSize.HasValue)
        {
            SetDefaultSize(savedSize.Value.Item1, savedSize.Value.Item2);
            return;
        }

        // Couldn't get monitor size. Use default size
        SetDefaultSize(width, height);
    }

    private void ResetNavigationView()
    {
        var visibleTag = _rootView.VisiblePageTag;
        while (visibleTag != "main_view")
        {
            _rootView.Pop();
            visibleTag = _rootView.VisiblePageTag;
        }
    }
    
    private void PlayerControllerOnShowShowLyricsClicked(object? sender, AlbumArgs e)
    {
        var controller = new LyricsController(_controller.JellyTuneApiService, _controller.PlayerService);
        var lyrics = LyricsView.NewWithValues(controller);
        lyrics.Present(this);
        _ = controller.UpdateAsync();
        lyrics.OnClosed += LyricsOnClosed;
    }

    private void LyricsOnClosed(Dialog sender, EventArgs args)
    {
        sender.OnClosed -= LyricsOnClosed;
        sender.Dispose();
    }

    private void PlayerControllerOnShowPlaylistClicked(object? sender, AlbumArgs e)
    {
        ResetNavigationView();
        _queueListController.Open();
        _rootView.Push(_queueList);
    }

    private void SearchControllerOnAlbumClicked(object? sender, AlbumArgs args)
    {
        _ = _albumController.OpenAsync(args.AlbumId, args.TrackId);
        _rootView.Push(_albumDetails);
    }

    private void AlbumListControllerOnAlbumClicked(object? sender, Guid albumId)
    {
        _ = _albumController.OpenAsync(albumId);
        _rootView.Push(_albumDetails);
    }

    private void ActRefreshOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        _ = RefreshLists(true);
    }

    private void ActAboutOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var about = AboutDialog.New();
        about.ApplicationName = _controller.ApplicationInfo.Name;
        about.ApplicationIcon = _controller.ApplicationInfo.Icon;
        about.DeveloperName = _controller.ApplicationInfo.Developer;
        about.Version = $"{Assembly.GetExecutingAssembly().GetName().Version?.Major}.{Assembly.GetExecutingAssembly().GetName().Version?.Minor}.{Assembly.GetExecutingAssembly().GetName().Version?.Build}";
        about.Website = _controller.ApplicationInfo.Website;
        about.IssueUrl = _controller.ApplicationInfo.IssueUrl;
        about.LicenseType = License.Gpl30;
        about.Designers = _controller.ApplicationInfo.Designers ?? [];
        about.Artists = _controller.ApplicationInfo.Artists ?? [];
        about.Present(this);
        about.OnClosed += AboutOnClosed;
    }

    private void AboutOnClosed(Dialog sender, EventArgs args)
    {
        sender.OnClosed -= AboutOnClosed;
        sender.Dispose();
    }

    /// <summary>
    /// Show preferences dialog
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void ActPreferencesOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        // Pause playing. Playing would break account related stuff
        _controller.PlayerService.StopTrack();
        
        var preferences = PreferencesView.NewWithValues(_controller.ConfigurationService, _controller.JellyTuneApiService);
        preferences.Present(this);
        preferences.OnClosed += async (_, _) =>
        {
            if (preferences.Refresh)
            {
                await _startupController.StartAsync(preferences.Password);
                await RefreshLists();
            }
        };
    }
    
    private void ActShowSearchBarOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        ResetNavigationView();
        
        _rootView.Push(_searchAlbums);
        _searchField.SetText(string.Empty);
        _searchField.GrabFocus();
        _searchController.StartSearch();
    }

    /// <summary>
    /// Starts the MainWindow
    /// </summary>
    public async Task StartAsync()
    {
        _application.AddWindow(this);
        Present();

        // Open dbus session
        await _mediaPlayerService.ConnectAsync();
       
        var startupState = await _startupController.StartAsync();
        if (startupState != StartupState.Finished)
        {
            var taskCompletionSource = new TaskCompletionSource();
            _spinner.SetVisible(false);
            var startup = StartupView.NewWithValues(_application, startupState, _startupController, taskCompletionSource);
            startup.Present(this);
            await taskCompletionSource.Task;
        }
        
        await UpdateMainMenu();
        await RefreshLists();
    }
    
    public override void Dispose()
    {
        _controller.PlayerService.OnPlayerStateChanged -= OnPlayerStateChanged;
        _searchField.OnSearchChanged -= SearchFieldOnSearchChanged;
        
        _playerController.Dispose();
        _albumController.Dispose();
        _searchController.Dispose();
        _playlistController.Dispose();
        _startupController.Dispose();
        _mediaPlayerService.Dispose();
        _queueListController.Dispose();
        base.Dispose();
    }

    /// <summary>
    /// Occurs when quit action is triggered
    /// </summary>
    /// <param name="sender">Gio.SimpleAction</param>
    /// <param name="e">EventArgs</param>
    private void Quit(SimpleAction sender, EventArgs e) => _application.Quit();
}
