using JellyTune.Shared.Controls;
using JellyTune.Shared.Services;
using AlertDialog = Adw.AlertDialog;

namespace JellyTune.Gnome.Views;


[GObject.Subclass<Adw.PreferencesDialog>(qualifiedName: "JellyTunePreferencesView")]
[Gtk.Template<Gtk.AssemblyResource>("JellyTune.Gnome.Blueprints.preferences.ui")]
public partial class PreferencesView
{
    private IConfigurationService _configurationService;
    private IJellyTuneApiService _jellyTuneApiService;

    private AccountController  _accountController;
    private AccountView _accountView;
    
    [Gtk.Connect] private Adw.PreferencesPage _preferencesPage1;

    [Gtk.Connect] private Adw.SwitchRow _cacheList;
    [Gtk.Connect] private Adw.SwitchRow _cacheArtwork;
    [Gtk.Connect] private Adw.SwitchRow _showListSeparator;
    [Gtk.Connect] private Adw.SwitchRow _showSeek;
    [Gtk.Connect] private Adw.SwitchRow _showVolume;
    [Gtk.Connect] private Adw.SwitchRow _showPlayingAlbum;
    [Gtk.Connect] private Adw.SwitchRow _showLyrics;

    public bool Refresh { get; set; } = false;
    public string? Password { get; set; } = null;

    private void CloseAttempt(Adw.Dialog sender, EventArgs args)
    {
        // We need to validate account so application won't break
        if (_accountController.IsValid())
        {
            var configuration = _configurationService.Get();
            configuration.CacheListData = _cacheList.GetActive();
            configuration.CacheAlbumArt = _cacheArtwork.GetActive();
            configuration.ShowListSeparator = _showListSeparator.GetActive();
            configuration.ShowLyrics = _showLyrics.GetActive();
            configuration.ShowSeek = _showSeek.GetActive();
            configuration.ShowVolume = _showVolume.GetActive();
            configuration.ShowCurrentAlbum = _showPlayingAlbum.GetActive();

            Refresh = _accountController.HasChanges();
            configuration.ServerUrl = _accountController.ServerUrl;
            configuration.Username = _accountController.Username;
            configuration.Password = _accountController.Password;
            configuration.CollectionId = _accountController.CollectionId?.ToString() ?? throw new NullReferenceException("This should never happen!");
            configuration.PlaylistCollectionId = _accountController.PlaylistCollectionId?.ToString();
            
            _configurationService.Set(configuration);
            _configurationService.Save();
            ForceClose();
        }
        else
        {
            var alert = PreferencesAlert.NewWithValues();
            alert.Present(this);
            alert.OnResponse += AlertOnResponse;
        }
    }

    private void AlertOnResponse(AlertDialog sender, AlertDialog.ResponseSignalArgs args)
    {
        if (args.Response == "close")
            ForceClose();
    }

    public static PreferencesView NewWithValues(IConfigurationService configurationService, IJellyTuneApiService jellyTuneApiService)
    {
        var obj = NewWithProperties([]);
        obj._configurationService = configurationService;
        obj._jellyTuneApiService = jellyTuneApiService;
        obj.InitializeController();
        return obj;
    }
    
    private void InitializeController()
    {
        _accountController = new AccountController(_configurationService, _jellyTuneApiService);
        _accountView =  AccountView.NewWithValues(_accountController);
        _preferencesPage1.Insert(_accountView, 0);
        
        var configuration =  _configurationService.Get();
        _accountController.OpenConfiguration(configuration, true);
        _cacheList.SetActive(configuration.CacheListData);
        _cacheArtwork.SetActive(configuration.CacheAlbumArt);
        _showListSeparator.SetActive(configuration.ShowListSeparator);
        
        _showLyrics.SetActive(configuration.ShowLyrics);
        _showSeek.SetActive(configuration.ShowSeek);
        _showVolume.SetActive(configuration.ShowVolume);
        _showPlayingAlbum.SetActive(configuration.ShowCurrentAlbum);
        
        OnCloseAttempt += CloseAttempt;
    }

    public override void Dispose()
    {
        OnCloseAttempt -= CloseAttempt;
        base.Dispose();
    }
}