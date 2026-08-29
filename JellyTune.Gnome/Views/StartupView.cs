using Adw;
using GObject;
using Gtk;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Enums;
using Application = Adw.Application;
using Dialog = Adw.Dialog;

namespace JellyTune.Gnome.Views;

[Subclass<Dialog>(qualifiedName: "JellyTuneStartupView")]
[Template<AssemblyResource>("JellyTune.Gnome.Blueprints.startup.ui")]
public partial class StartupView
{
    private Application _application;
    private StartupController  _controller;

    private AccountController   _accountController;
    private AccountView _accountView;

    private TaskCompletionSource _taskCompletionSource;
    private StartupState _startupState;
    
    [Connect] private Carousel _carousel;
    
    [Connect] private Button _close;
    [Connect] private Button _continue0;
    
    [Connect] private Button _back;
    [Connect] private Box _accountBox;
    [Connect] private Button _continue1;

    public static StartupView NewWithValues(Application application, StartupState startupState, StartupController controller,
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
        _accountController.OnUpdate += (_, b) =>
        {
            _continue1.SetSensitive(b);
        };

        _close.OnClicked += (_, _) =>
        {
            _application.Quit();
        };
        
        _continue0.OnClicked += (_, _) =>
        {
            _carousel.ScrollTo(_carousel.GetNthPage(1), true);
        };

        _back.OnClicked += (_, _) =>
        {
            _carousel.ScrollTo(_carousel.GetNthPage(0), true);
        };
        
        // Save configuration
        _continue1.OnClicked += async (_, _) =>
        {
            _continue1.SetSensitive(false);
            var configuration = _controller.ConfigurationService.Get();
            configuration.ServerUrl = _accountController.ServerUrl ?? string.Empty;
            configuration.Username = _accountController.Username ?? string.Empty;
            configuration.Password = _accountController.Password;

            if (_accountController.CollectionId != null)
                configuration.CollectionId = _accountController.CollectionId.ToString() ?? throw new Exception("CollectionId is null when it should not!");
            
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