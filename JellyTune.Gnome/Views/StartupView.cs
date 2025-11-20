using Adw.Internal;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Enums;
using JellyTune.Gnome.Helpers;

namespace JellyTune.Gnome.Views;

public class StartupView : Adw.Dialog
{
    private readonly StartupController  _controller;

    private readonly AccountController   _accountController;
    private readonly AccountView _accountView;

    private readonly TaskCompletionSource _taskCompletionSource;
    
    [Gtk.Connect] private readonly Adw.Carousel _carousel;
    
    [Gtk.Connect] private readonly Gtk.Button _continue0;
    
    [Gtk.Connect] private readonly Gtk.Box _accountBox;
    [Gtk.Connect] private readonly Gtk.Button _continue1;
    
    private StartupView(Gtk.Builder builder) : base(
        new DialogHandle(builder.GetPointer("_root"), false))
    {
        builder.Connect(this);
    }

    public StartupView(StartupState startupState, StartupController controller,
        TaskCompletionSource taskCompletionSource) : this(Blueprint.BuilderFromFile("startup"))
    {
        _controller = controller;
        _taskCompletionSource = taskCompletionSource;
        
        _accountController = new AccountController(_controller.GetConfigurationService(), _controller.GetJellyTuneApiService());
        _accountView = new AccountView(_accountController);
        _accountController.OpenConfiguration(_controller.GetConfigurationService().Get(), startupState != StartupState.InitialRun);
        _accountBox.Prepend(_accountView);
        _accountController.OnUpdate += (sender, b) =>
        {
            _continue1.SetSensitive(b);
        };
        
        _continue0.OnClicked += (sender, args) =>
        {
            _carousel.ScrollTo(_carousel.GetNthPage(1), true);
        };

        // Save configuration
        _continue1.OnClicked += async (sender, args) =>
        {
            _continue1.SetSensitive(false);
            var configuration = _controller.GetConfigurationService().Get();
            configuration.ServerUrl = _accountController.ServerUrl;
            configuration.Username = _accountController.Username;

            if (_accountController.RememberPassword)
            {
                configuration.Password = _accountController.Password;
                configuration.RememberPassword  = true;
            }
            else
            {
                configuration.Password = string.Empty;
                configuration.RememberPassword = false;
            }

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