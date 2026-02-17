using Adw.Internal;
using Gtk;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Enums;
using JellyTune.Gnome.Helpers;
using Object = GObject.Object;

namespace JellyTune.Gnome.Views;

public class LoginView : Adw.Dialog
{
    private StartupController  _controller;

    [Gtk.Connect] private readonly Adw.PasswordEntryRow _password;
    [Gtk.Connect] private readonly Gtk.Button _continue;

    private string _passwordValue;
    private TaskCompletionSource _taskCompletionSource;
    
    private LoginView(Gtk.Builder builder) : base(
        new DialogHandle(builder.GetPointer("_root"), false))
    {
        builder.Connect(this);
    }
    
    public LoginView(StartupController controller, TaskCompletionSource taskCompletionSource) : this(GtkHelper.BuilderFromFile("login"))
    {
        _controller = controller;
        _taskCompletionSource  = taskCompletionSource;
        
        _password.OnNotify += PasswordOnNotify;
        _continue.OnClicked += ContinueOnClicked;
    }

    private async void ContinueOnClicked(Button sender, EventArgs args)
    {
        _continue.SetSensitive(false);
        _password.SetSensitive(false);
        _password.RemoveCssClass("error");
        _passwordValue = _password.GetText().Trim();
        var startupState = await _controller.StartAsync(_passwordValue);
        if (startupState == StartupState.Finished)
        {
            _taskCompletionSource.SetResult();
            ForceClose();
        }
        else
        {
            _password.SetSensitive(true);
            _password.AddCssClass("error");
        }
    }

    private void PasswordOnNotify(Object sender, NotifySignalArgs args)
    {
        var text = _password.GetText().Trim();
        if (!string.IsNullOrWhiteSpace(text))
        {
            if (text != _passwordValue)
                _password.RemoveCssClass("error");
                
            _continue.SetSensitive(true);
        }
        else
        {
            _continue.SetSensitive(false);
        }
    }

    public override void Dispose()
    {
        _password.OnNotify -= PasswordOnNotify;
        _continue.OnClicked -= ContinueOnClicked;
        base.Dispose();
    }
}