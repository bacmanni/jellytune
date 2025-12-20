using Adw.Internal;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Services;
using JellyTune.Gnome.Helpers;
using AlertDialog = Adw.AlertDialog;

namespace JellyTune.Gnome.Views;

public partial class PreferencesView : Adw.PreferencesDialog
{
    private readonly IConfigurationService _configurationService;
    private readonly IJellyTuneApiService _jellyTuneApiService;

    private readonly AccountController  _accountController;
    private readonly AccountView _accountView;
    
    [Gtk.Connect] private readonly Adw.PreferencesPage _preferencesPage1;

    [Gtk.Connect] private readonly Adw.SwitchRow _cacheList;
    [Gtk.Connect] private readonly Adw.SwitchRow _cacheArtwork;
    [Gtk.Connect] private readonly Adw.SwitchRow _showListSeparator;
    
    public bool Refresh { get; set; } = false;
    public string? Password { get; set; } = null;
    
    private PreferencesView(Gtk.Builder builder) : base(
        new PreferencesDialogHandle(builder.GetPointer("_root"), false))
    {
        builder.Connect(this);
        OnCloseAttempt += CloseAttempt;
    }

    private void CloseAttempt(Adw.Dialog sender, EventArgs args)
    {
        // We need to validate account so application won't break
        if (_accountController.IsValid())
        {
            var configuration = _configurationService.Get();
            configuration.CacheListData = _cacheList.GetActive();
            configuration.CacheAlbumArt = _cacheArtwork.GetActive();
            configuration.ShowListSeparator = _showListSeparator.GetActive();
            
            Refresh = _accountController.HasChanges();
            configuration.ServerUrl = _accountController.ServerUrl;
            configuration.Username = _accountController.Username;

            // If password is not saved, we pass it temporarily through variable
            if (_accountController.RememberPassword)
            {
                configuration.Password = _accountController.Password;
            }
            else
            {
                configuration.Password = string.Empty;
                Password = _accountController.Password;
            }
            
            configuration.RememberPassword = _accountController.RememberPassword;
            configuration.CollectionId = _accountController.CollectionId?.ToString() ?? throw new NullReferenceException("This should never happen!");
            configuration.PlaylistCollectionId = _accountController.PlaylistCollectionId?.ToString();
            
            _configurationService.Set(configuration);
            _configurationService.Save();
            ForceClose();
        }
        else
        {
            var alert = new PreferencesAlert();
            alert.Present(this);
            alert.OnResponse += AlertOnResponse;
        }
    }

    private void AlertOnResponse(AlertDialog sender, AlertDialog.ResponseSignalArgs args)
    {
        if (args.Response == "close")
            ForceClose();
    }

    public PreferencesView(IConfigurationService configurationService, IJellyTuneApiService jellyTuneApiService) : this(Blueprint.BuilderFromFile("preferences"))
    {
        _configurationService = configurationService;
        _jellyTuneApiService = jellyTuneApiService;
        
        _accountController = new AccountController(_configurationService, _jellyTuneApiService);
        _accountView =  new AccountView(_accountController);
        _preferencesPage1.Add(_accountView);
        
        var configuration =  _configurationService.Get();
        _accountController.OpenConfiguration(configuration, true);
        _cacheList.SetActive(configuration.CacheListData);
        _cacheArtwork.SetActive(configuration.CacheAlbumArt);
        _showListSeparator.SetActive(configuration.ShowListSeparator);
    }

    public override void Dispose()
    {
        OnCloseAttempt -= CloseAttempt;
        base.Dispose();
    }
}