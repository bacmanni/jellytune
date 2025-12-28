using System.Runtime.InteropServices;
using Gio;
using GLib;
using Gtk;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using JellyTune.Gnome.Helpers;
using JellyTune.Gnome.MediaPlayer;
using Object = GObject.Object;
using Task = System.Threading.Tasks.Task;

namespace JellyTune.Gnome.Views;

/// <summary>
/// The MainWindow for the application
/// </summary>
public partial class MainWindow : Adw.ApplicationWindow
{
    private readonly MainWindowController _controller;
    private readonly Adw.Application _application;
    
    private readonly StartupController _startupController;

    private readonly MediaPlayerController _mediaPlayerController;
    
    private readonly PlayerController _playerController;
    private readonly PlayerView  _playerView;
    
    private readonly AlbumController _albumController;
    private readonly AlbumView _albumView;
    
    private readonly AlbumlistController _albumlistController;
    private readonly AlbumListView _albumListView;
    
    private readonly SearchController _searchController;
    private readonly SearchView _searchView;
    
    private readonly QueueListController _queueListController;
    private readonly QueueListView _queueListView;
    
    private readonly PlaylistController _playlistController;
    private readonly PlaylistView _playlistView;

    private readonly PlaylistTracksController _playlistTracksController;
    private readonly PlaylistTracksView _playlistTracksView;

    private readonly int _breakpoint = 500;
    
    private readonly Gio.SimpleAction _refreshAction;

    private CancellationTokenSource? _menuUpdateCancellationTokenSource;
    
    [Gtk.Connect] private readonly Gtk.Button _searchButton;
    [Gtk.Connect] private readonly Gtk.SearchEntry _search_field;
    
    [Gtk.Connect] private readonly Gtk.Box _player;

    //[Gtk.Connect] private readonly Adw.ToastOverlay toastOverlay;
    
    [Gtk.Connect] private readonly Gtk.MenuButton _menuButton;
    [Gtk.Connect] private readonly Adw.Spinner _spinner;
    
    [Gtk.Connect] private readonly Adw.NavigationPage _main_view;
    [Gtk.Connect] private readonly Adw.NavigationPage _album_details;
    [Gtk.Connect] private readonly Adw.NavigationPage _search_albums;
    [Gtk.Connect] private readonly Adw.NavigationPage _queue_list;
    [Gtk.Connect] private readonly Adw.NavigationPage _playlist_tracks;
    
    [Gtk.Connect] private readonly Adw.NavigationView _album_view;
    [Gtk.Connect] private readonly Adw.ToolbarView _album_list_view;
    [Gtk.Connect] private readonly Adw.ToolbarView _album_details_view;
    [Gtk.Connect] private readonly Adw.ToolbarView _search_albums_view;
    [Gtk.Connect] private readonly Adw.ToolbarView _queue_list_view;
    [Gtk.Connect] private readonly Adw.ToolbarView _playlist_tracks_view;
    
    [Gtk.Connect] private readonly Adw.HeaderBar _main_view_headerbar;
    [Gtk.Connect] private readonly Adw.ViewStack _main_stack;
    
    [Gtk.Connect] private readonly Gtk.Box _main_stack_music;
    [Gtk.Connect] private readonly Gtk.Box _main_stack_playlist;
    
    // This is stupid hack. Used for displaying shadow correctly on player
    [Gtk.Connect] private readonly Gtk.Box _main_view_footer;
    [Gtk.Connect] private readonly Gtk.Box _album_details_footer;
    [Gtk.Connect] private readonly Gtk.Box _search_albums_footer;
    [Gtk.Connect] private readonly Gtk.Box _queue_list_footer;
    [Gtk.Connect] private readonly Gtk.Box _playlist_tracks_footer;
    
    [Gtk.Connect] private readonly Gtk.Button _queue_list_shuffle;
    
    private MainWindow(Gtk.Builder builder, MainWindowController controller, Adw.Application application) : base(new Adw.Internal.ApplicationWindowHandle(builder.GetPointer("_root"), false))
    {
        //Window Settings
        _controller = controller;
        _application = application;
        SetIconName(_controller.ApplicationInfo.Icon);
        SetWindowSize(360, 600);
        
        //Build UI
        builder.Connect(this);

        _queue_list_shuffle.OnClicked += (sender, args) =>
        {
            _queueListController.ShuffleTracks();
        };

        _controller.GetPlayerService().OnPlayerStateChanged += OnPlayerStateChanged;

        // Album list
        _albumlistController = new AlbumlistController(_controller.GetJellyTuneApiService(),
            _controller.GetConfigurationService(), _controller.GetPlayerService(), _controller.GetFileService());
        _albumlistController.OnAlbumClicked += AlbumlistControllerOnAlbumClicked;
        _albumListView = new AlbumListView(_albumlistController);
        _main_stack_music.Append(_albumListView);
        
        //Album details
        _albumController = new AlbumController(_controller.GetJellyTuneApiService(), _controller.GetConfigurationService(), _controller.GetPlayerService(), _controller.GetFileService());
        _albumView = new AlbumView(_albumController);
        _album_details_view.SetContent(_albumView);
        
        // Startup
        _startupController = new StartupController(_controller.GetJellyTuneApiService(), _controller.GetConfigurationService());

        // Media controls
        _mediaPlayerController = new MediaPlayerController(this, _controller.GetFileService(), _controller.GetPlayerService(), _controller.ApplicationInfo);
        
        //Audio player
        _playerController = new PlayerController(_controller.GetJellyTuneApiService(), _controller.GetConfigurationService(), _controller.GetPlayerService());
        _playerController.OnShowPlaylistClicked += PlayerControllerOnShowPlaylistClicked;
        _playerController.OnShowShowLyricsClicked += PlayerControllerOnShowShowLyricsClicked;
        _playerView = new PlayerView(this, _playerController);
        _player.Append(_playerView);
        
        // Search
        _searchController = new SearchController(_controller.GetJellyTuneApiService(), _controller.GetConfigurationService(), _controller.GetPlayerService(), _controller.GetFileService());
        _searchController.OnAlbumClicked += SearchControllerOnAlbumClicked;
        _searchView = new SearchView(_searchController);
        _search_albums_view.SetContent(_searchView);
        _search_field.OnSearchChanged += SearchFieldOnSearchChanged;
        
        // Que list for currently playling queue
        _queueListController = new QueueListController(_controller.GetJellyTuneApiService(), _controller.GetConfigurationService(), _controller.GetPlayerService(), _controller.GetFileService());
        _queueListView = new QueueListView(_queueListController);
        _queue_list_view.SetContent(_queueListView);
        
        // Playlist
        _playlistController = new PlaylistController(_controller.GetJellyTuneApiService(), _controller.GetConfigurationService(), _controller.GetPlayerService(), _controller.GetFileService());
        _playlistView = new PlaylistView(_playlistController);
        _main_stack_playlist.Append(_playlistView);
        _playlistController.OnPlaylistClicked += PlaylistControllerOnPlaylistClicked;
        
        _playlistTracksController = new PlaylistTracksController(_controller.GetJellyTuneApiService(), _controller.GetConfigurationService(), _controller.GetPlayerService(), _controller.GetFileService());
        _playlistTracksView = new PlaylistTracksView(_playlistTracksController);
        _playlist_tracks_view.SetContent(_playlistTracksView);

        //Refresh application
        _refreshAction = Gio.SimpleAction.New("refresh", null);
        _refreshAction.OnActivate += ActRefreshOnActivate;
        AddAction(_refreshAction);
        application.SetAccelsForAction("win.refresh", new string[] { "<Ctrl>r" });

        //Preferences Action
        var actPreferences = Gio.SimpleAction.New("preferences", null);
        actPreferences.OnActivate += ActPreferencesOnOnActivate;
        AddAction(actPreferences);
        
        //About Action
        var actAbout = Gio.SimpleAction.New("about", null);
        actAbout.OnActivate += ActAboutOnOnActivate;
        AddAction(actAbout);

        //Search
        var actSearchBar = Gio.SimpleAction.New("search", null);
        actSearchBar.OnActivate += ActShowSearchBarOnOnActivate;
        AddAction(actSearchBar);
        application.SetAccelsForAction("win.search", new string[] { "<Ctrl>f" });

        //Quit Action
        var actQuit = Gio.SimpleAction.New("quit", null);
        actQuit.OnActivate += Quit;
        AddAction(actQuit);
        application.SetAccelsForAction("win.quit", new string[] { "<Ctrl>q" });
        
        // Event for ctrl click
        var ctrlEvent = Gtk.EventControllerKey.New();
        ctrlEvent.OnKeyPressed += (sender, args) =>
        {
            _albumView.IsCtrlActive(true);
            return true;
        };

        ctrlEvent.OnKeyReleased += (sender, args) =>
        {
            _albumView.IsCtrlActive(false);
        };
        
        AddController(ctrlEvent);
        
        // Event for selected view
        var viewAction = SimpleAction.NewStateful(
            "view",
            VariantType.String,
            Variant.NewString("page1")
        );
        
        viewAction.OnChangeState += (sender, args) =>
        {
            viewAction.SetState(args.Value);
            var newState = args.Value.Print(false).Trim('\'');
            
            if (_main_stack.VisibleChildName != newState)
                _main_stack.SetVisibleChildName(newState);
        };
        AddAction(viewAction);
        application.SetAccelsForAction("win.view('page1')", new string[] { "<Ctrl>1" });
        application.SetAccelsForAction("win.view('page2')", new string[] { "<Ctrl>2" });
        
        OnNotify += OnOnNotify;
        _ = UpdateMainMenu();
    }

    private void PlaylistControllerOnPlaylistClicked(object? sender, Guid id)
    {
        var visiblePageName = _album_view.GetVisiblePage()?.Tag;
        if (visiblePageName == "_playlist_tracks") return;

        _ = _playlistTracksController.OpenPlaylist(id);
        _album_view.Push(_playlist_tracks);
    }

    private void OnOnNotify(Object sender, NotifySignalArgs args)
    {
        if (args.Pspec.GetName() != "default-width" && args.Pspec.GetName() != "maximized") return;
        _ = UpdateMainMenu(args.Pspec.GetName() == "maximized");
    }

    private async Task RefreshLists(bool reload = false)
    {
        _refreshAction.SetEnabled(false);
        await _albumlistController.Refresh(reload);
        await _playlistController.Refresh(reload);
        _refreshAction.SetEnabled(true);
    }
    
    private async Task UpdateMainMenu(bool delay = false)
    {
        _menuUpdateCancellationTokenSource?.Cancel();
        _menuUpdateCancellationTokenSource = new CancellationTokenSource();
        var width1 = GetAllocatedWidth();
        
        if (delay)
        {
            await Task.Delay(50, _menuUpdateCancellationTokenSource.Token);
            var width2 = GetAllocatedWidth();

            while (width1 != width2)
            {
                width1 = GetAllocatedWidth();
                await Task.Delay(50, _menuUpdateCancellationTokenSource.Token);
                width2 = GetAllocatedWidth();
            }
        
            if (_menuUpdateCancellationTokenSource.IsCancellationRequested) return;
        }
        
        var show = width1 < _breakpoint;
        var mainMenu = _menuButton.MenuModel as Gio.Menu;
        var existingSection = mainMenu.GetItemLink(0, "section") as Gio.Menu;
        var hasSection = existingSection.GetItemAttributeValue(0, "action", VariantType.String).Print(false)
            .Trim('\'').Contains("win.view");
        if (show)
        {
            _main_view_headerbar.TitleWidget?.SetVisible(false);
            
            if (!hasSection)
            {
                var section = new Gio.Menu();
                section.Insert(0, "Music", "win.view('page1')");
                section.Insert(1, "Playlist", "win.view('page2')");
                mainMenu.InsertSection(0, null, section);
            }
        }
        else if (!show)
        {
            _main_view_headerbar.TitleWidget?.SetVisible(true);

            if (hasSection)
            {
                mainMenu.Remove(0);
            }
        }
    }
    
    private void OnPlayerStateChanged(object? sender, PlayerStateArgs args)
    {
        if (args.State is PlayerState.Playing or PlayerState.Stopped or PlayerState.Paused)
        {
            if (!_main_view_footer.IsVisible())
                _main_view_footer.SetVisible(true);
                
            if (!_album_details_footer.IsVisible())
                _album_details_footer.SetVisible(true);
                
            if (!_player.IsVisible())
                _player.SetVisible(true);
                
            if (!_search_albums_footer.IsVisible())
                _search_albums_footer.SetVisible(true);
                
            if (!_queue_list_footer.IsVisible())
                _queue_list_footer.SetVisible(true);

            if (!_playlist_tracks_footer.IsVisible())
                _playlist_tracks_footer.SetVisible(true);
        }
        else if (args.State is PlayerState.None)
        {
            _player?.SetVisible(false);
            _main_view_footer?.SetVisible(false);
            _album_details_footer?.SetVisible(true);
            _search_albums_footer?.SetVisible(true);
            _queue_list_footer?.SetVisible(false);
            _playlist_tracks_footer?.SetVisible(false);
        }
    }

    private void SearchFieldOnSearchChanged(SearchEntry sender, EventArgs args)
    {
        if (string.IsNullOrWhiteSpace(sender.GetText()))
        {
            _searchController.StartSearch();
            return;
        }
        
        _ = _searchController.SearchAlbums(sender.GetText());
    }
    
    public (int, int) GetScreenSize()
    {
        return (DefaultWidth, DefaultHeight);
    }

    
    private Gdk.Rectangle? GetScreenRectangle()
    {
        if (Display == null) return null;
        
        var monitors = Display.GetMonitors();
        for (uint n = 0; n < monitors.GetNItems(); n++)
        {
            var monitor = monitors.GetObject(n) as Gdk.Monitor;
            
            if (monitor == null)
                continue;

            return monitor.Geometry;
        }
        
        return null;
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

    private void PlayerControllerOnShowShowLyricsClicked(object? sender, AlbumArgs e)
    {
        var controller = new LyricsController(_controller.GetJellyTuneApiService(), _controller.GetPlayerService());
        var lyrics = new LyricsView(controller);
        lyrics.Present(this);
        _ = controller.Update();
    }

    private void PlayerControllerOnShowPlaylistClicked(object? sender, AlbumArgs e)
    {
        var visiblePageName = _album_view.GetVisiblePage()?.Tag;
        _album_view.Pop();
        
        if (visiblePageName == "_queue_list") return;
        
        _queueListController.Open();
        _album_view.Push(_queue_list);
    }

    private void SearchControllerOnAlbumClicked(object? sender, AlbumArgs args)
    {
        var visiblePageName = _album_view.GetVisiblePage()?.Tag;
        if (visiblePageName != "_search_albums")
            _album_view.Pop();
        
        _albumController.Open(args.AlbumId, args.TrackId);
        _album_view.Push(_album_details);
    }

    private void AlbumlistControllerOnAlbumClicked(object? sender, Guid albumId)
    {
        var visiblePageName = _album_view.GetVisiblePage()?.Tag;
        if (visiblePageName != "_album_details")
            _album_view.Pop();
        
        _ = _albumController.Open(albumId);
        _album_view.Push(_album_details);
    }

    private void ActRefreshOnActivate(Gio.SimpleAction sender, Gio.SimpleAction.ActivateSignalArgs args)
    {
        _ = RefreshLists(true);
    }

    private void ActAboutOnOnActivate(Gio.SimpleAction sender, Gio.SimpleAction.ActivateSignalArgs args)
    {
        var about = Adw.AboutDialog.New();
        about.ApplicationName = _controller.ApplicationInfo.Name;
        about.ApplicationIcon = _controller.ApplicationInfo.Icon;
        about.DeveloperName = _controller.ApplicationInfo.Developer;
        about.Version = _controller.ApplicationInfo.Version;
        about.Website = _controller.ApplicationInfo.Website;
        about.Copyright = _controller.ApplicationInfo.Copyright;
        about.IssueUrl = _controller.ApplicationInfo.IssueUrl;
        about.ReleaseNotes = _controller.ApplicationInfo.ReleaseNotes;
        about.LicenseType = License.Gpl30;
        about.Designers = _controller.ApplicationInfo.Designers;
        about.Artists = _controller.ApplicationInfo.Artists;
        about.Present(this);
    }

    /// <summary>
    /// Show preferences dialog
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void ActPreferencesOnOnActivate(Gio.SimpleAction sender, Gio.SimpleAction.ActivateSignalArgs args)
    {
        var preferences = new PreferencesView(_controller.GetConfigurationService(), _controller.GetJellyTuneApiService());
        preferences.Present(this);
        preferences.OnClosed += async (dialog, eventArgs) =>
        {
            if (preferences.Refresh)
            {
                await _startupController.StartAsync(preferences.Password);
                await RefreshLists();
            }
        };
    }
    
    private void ActShowSearchBarOnOnActivate(Gio.SimpleAction sender, Gio.SimpleAction.ActivateSignalArgs args)
    {
        var visiblePageName = _album_view.GetVisiblePage()?.Tag;
        if (visiblePageName != "_search_albums")
            _album_view.Pop();
        
        _album_view.Push(_search_albums);
        _search_field.SetText(string.Empty);
        _search_field.GrabFocus();
        _searchController.StartSearch();
    }

    /// <summary>
    /// Constructs a MainWindow
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="controller">The MainWindowController</param>
    /// <param name="application">The Adw.Application</param>
    public MainWindow(Adw.Application sender, MainWindowController controller, Adw.Application application) : this(Blueprint.BuilderFromFile("window"), controller, application)
    {
    }

    /// <summary>
    /// Starts the MainWindow
    /// </summary>
    public async Task StartAsync()
    {
        _application.AddWindow(this);
        Present();

        var startupState = await _startupController.StartAsync();
        if (startupState == StartupState.RequirePassword)
        {
            var taskCompletionSource = new TaskCompletionSource();
            _spinner.SetVisible(false);
            var login = new LoginView(_startupController, taskCompletionSource);
            login.Present(this);
            await taskCompletionSource.Task;
        }
        else if (startupState != StartupState.Finished)
        {
            var taskCompletionSource = new TaskCompletionSource();
            _spinner.SetVisible(false);
            var startup = new StartupView(startupState, _startupController, taskCompletionSource);
            startup.Present(this);
            await taskCompletionSource.Task;
        }

        _spinner.SetVisible(false);
        _album_view.SetVisible(true);
        
        if(_controller.GetConfigurationService().IsPlatform(OSPlatform.Linux))
            await _mediaPlayerController.ConnectAsync();

        await RefreshLists();
    }
    
    public override void Dispose()
    {
        _controller.GetPlayerService().OnPlayerStateChanged -= OnPlayerStateChanged;
        _search_field.OnSearchChanged -= SearchFieldOnSearchChanged;
        
        _albumController.Dispose();
        _playerController.Dispose();
        _albumController.Dispose();
        _searchController.Dispose();
        _playlistController.Dispose();
        _startupController.Dispose();
        _mediaPlayerController.Dispose();
        _queueListController.Dispose();
        base.Dispose();
    }

    /// <summary>
    /// Occurs when quit action is triggered
    /// </summary>
    /// <param name="sender">Gio.SimpleAction</param>
    /// <param name="e">EventArgs</param>
    private void Quit(Gio.SimpleAction sender, EventArgs e) => _application.Quit();
}
