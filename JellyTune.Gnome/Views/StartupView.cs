using JellyTune.Shared.Controls;
using JellyTune.Shared.Enums;

namespace JellyTune.Gnome.Views;

[GObject.Subclass<Adw.Dialog>(qualifiedName: "JellyTuneStartupView")]
[Gtk.Template<Gtk.AssemblyResource>("JellyTune.Gnome.Blueprints.startup.ui")]
public partial class StartupView
{
    private Adw.Application _application;
    private StartupController  _controller;

    private AccountController   _accountController;
    private AccountView _accountView;

    private TaskCompletionSource _taskCompletionSource;
    private StartupState _startupState;
    
    [Gtk.Connect] private Adw.Carousel _carousel;
    
    [Gtk.Connect] private Gtk.Button _close;
    [Gtk.Connect] private Gtk.Button _continue0;
    
    [Gtk.Connect] private Gtk.Button _back;
    [Gtk.Connect] private Gtk.Box _accountBox;
    [Gtk.Connect] private Gtk.Button _continue1;

    public static StartupView NewWithValues(Adw.Application application, StartupState startupState, StartupController controller,
        TaskCompletionSource taskCompletionSource)
    {
        var obj = NewWithProperties([]);
        obj._application = application;
        obj._controller = controller;
        obj._taskCompletionSource = taskCompletionSource;
        obj._startupState = startupState;
        obj.InitializeController();
        return obj;
    }
    
    private void InitializeController()
    {
        _accountController = new AccountController(_controller.ConfigurationService, _controller.JellyTuneApiService);
        _accountView = AccountView.NewWithValues(_accountController);
        _accountController.OpenConfiguration(_controller.ConfigurationService.Get(), _startupState != StartupState.InitialRun);
        _accountBox.Prepend(_accountView);
        _accountController.OnUpdate += (sender, b) =>
        {
            _continue1.SetSensitive(b);
        };

        _close.OnClicked += (sender, args) =>
        {
            _application.Quit();
        };
        
        _continue0.OnClicked += (sender, args) =>
        {
            _carousel.ScrollTo(_carousel.GetNthPage(1), true);
        };

        _back.OnClicked += (sender, args) =>
        {
            _carousel.ScrollTo(_carousel.GetNthPage(0), true);
        };
        
        // Save configuration
        _continue1.OnClicked += async (sender, args) =>
        {
            _continue1.SetSensitive(false);
            var configuration = _controller.ConfigurationService.Get();
            configuration.ServerUrl = _accountController.ServerUrl;
            configuration.Username = _accountController.Username;
            configuration.Password = _accountController.Password;

            if (_accountController.CollectionId != null)
                configuration.CollectionId = _accountController.CollectionId.ToString();
            
            if (_accountController.PlaylistCollectionId != null)
                configuration.PlaylistCollectionId = _accountController.PlaylistCollectionId.ToString();
            
            _controller.SaveConfiguration(configuration);
            var state = await _controller.StartAsync();
            if (state == StartupState.Finished)
            {
                _taskCompletionSource.SetResult();
                ForceClose();
            }
            else
            {
                _continue1.SetSensitive(false);
            }
        };
    }
}