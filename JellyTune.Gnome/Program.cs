using System.IO.Abstractions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using Gio;
using Jellyfin.Sdk;
using JellyTune.Gnome.Views;
using JellyTune.Shared.Controls;
using JellyTune.Shared.Handlers;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Application = Adw.Application;
using Functions = Gio.Functions;
using Module = Gtk.Module;

namespace JellyTune.Gnome;

class Program
{
    private readonly Application _application;
    private readonly IServiceProvider _serviceProvider;
    private readonly MainWindowController  _mainWindowController;
    private MainWindow? _mainWindow;

    private readonly ApplicationInfo _applicationInfo = new()
    {
        Id = "io.github.bacmanni.jellytune",
        Developer = "Joni Bäckström",
        Email = "joni.j.backstrom@gmail.com",
        Name = "JellyTune",
        Version = "1.0",
        Website = "https://github.com/bacmanni/jellytune",
        IssueUrl = "https://github.com/bacmanni/jellytune/issues/new",
        Icon = "jellytune-icon",
        ReleaseNotes = "<p>Initial release</p>",
        Artists = [ "Ruut Kiiskilä" ]
    };
    
    public static int Main(string[] args) => new Program().Run();
    private int Run()
    {
        try
        {
            return _application.RunWithSynchronizationContext([_applicationInfo.Id != null ? _applicationInfo.Id : throw new Exception("Missing application Id")]);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine($"\n\n{e.StackTrace}");
            return -1;
        }
    }

    private Program()
    {
        Module.Initialize();
        Adw.Module.Initialize();
        Gio.Module.Initialize();

        var serviceCollection = new ServiceCollection();
        ConfigureServices(_applicationInfo, serviceCollection);
        _serviceProvider = serviceCollection.BuildServiceProvider();
        
        var apiService = _serviceProvider.GetService<IJellyTuneApiService>();
        var playerService = _serviceProvider.GetService<IPlayerService>();
        var fileService = _serviceProvider.GetService<IFileService>();
        var configurationService = _serviceProvider.GetService<IConfigurationService>();

        if (configurationService == null)
            throw new Exception("Failed to get configuration service");
            
        configurationService.Load();
        var deviceId = configurationService.Get<string>("DeviceId");
        
        var sdkClientSettings = _serviceProvider.GetRequiredService<JellyfinSdkSettings>();
        sdkClientSettings.Initialize(
            _applicationInfo.Name != null ? _applicationInfo.Name : throw new Exception("Missing application name"),
            _applicationInfo.Version != null ? _applicationInfo.Version : string.Empty,
            "JellyTune Gnome",
            $"jellytune-{deviceId}");
        
        var resourceFile = Path.GetFullPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new Exception("Could not get executing assembly location")) + $"/{_applicationInfo.Id}.gresource";
        Functions.ResourcesRegister(Functions.ResourceLoad(resourceFile));
        
        if (apiService == null || playerService == null || fileService == null)
            throw new Exception("Failed to load required services");
        
        _mainWindowController = new MainWindowController(apiService, configurationService, playerService, fileService, _applicationInfo);
        
        _application = Application.New(_applicationInfo.Id, ApplicationFlags.NonUnique);
        _application.OnActivate += ApplicationOnOnActivate;
        _application.OnShutdown += ApplicationOnOnShutdown;
    }

    private async void ApplicationOnOnActivate(Gio.Application sender, EventArgs args)
    {
        if (_mainWindow != null)
        {
            _mainWindow.Present();
            return;
        }
        
        _mainWindow = MainWindow.NewWithValues(_mainWindowController, _application);
        await _mainWindow.StartAsync();
    }

    private void ApplicationOnOnShutdown(Gio.Application sender, EventArgs args)
    {
        if (_mainWindow != null)
        {
            var screenSize = _mainWindow.GetScreenSize();
            _mainWindowController.UpdateWindowSize(screenSize.Item1, screenSize.Item2);
        }
        
        _mainWindow?.Dispose();
        _mainWindowController.Dispose();
        
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private void ConfigureServices(ApplicationInfo applicationInfo, IServiceCollection serviceCollection)
    {
        // Basic http client
        serviceCollection.AddHttpClient("Default", c =>
            {
                c.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue(
                        applicationInfo.Name != null ? applicationInfo.Name : throw new Exception("Missing application name"),
                        applicationInfo.Version));
                c.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json, 1.0));
                c.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("*/*", 0.8));
            })
            .ConfigurePrimaryHttpMessageHandler(_ => new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                RequestHeaderEncodingSelector = (_, _) => Encoding.UTF8,
            }).AddHttpMessageHandler<HttpClientExceptionHandler>();

        // Logging
        serviceCollection.AddTransient<HttpClientExceptionHandler>();
        
        // Jellyfin sdk related
        serviceCollection.AddSingleton<JellyfinSdkSettings>();
        serviceCollection.AddSingleton<IAuthenticationProvider, JellyfinAuthenticationProvider>();
        serviceCollection.AddScoped<IRequestAdapter, JellyfinRequestAdapter>(s => new JellyfinRequestAdapter(
            s.GetRequiredService<IAuthenticationProvider>(),
            s.GetRequiredService<JellyfinSdkSettings>(),
            s.GetRequiredService<IHttpClientFactory>().CreateClient("Default")));
        serviceCollection.AddScoped<JellyfinApiClient>();
        
        // Project related
        serviceCollection.AddSingleton<IConfigurationService, ConfigurationService>(
            serviceProvider => new ConfigurationService(fileSystem: serviceProvider.GetRequiredService<IFileSystem>(), GLib.Functions.GetUserConfigDir(), GLib.Functions.GetUserCacheDir())
        );
        
        serviceCollection.AddSingleton<IFileSystem, FileSystem>();
        serviceCollection.AddSingleton<IJellyTuneApiService, JellyTuneApiService>();
        serviceCollection.AddSingleton<IPlayerService, PlayerService>();
        serviceCollection.AddSingleton<IFileService, FileService>();
        serviceCollection.AddSingleton<MainWindowController>();
    }
}