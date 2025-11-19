using System.IO.Abstractions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using JellyPlayer.Shared.Controls;
using Jellyfin.Sdk;
using JellyPlayer.Shared.Handlers;
using JellyPlayer.Shared.Models;
using JellyPlayer.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace JellyPlayer.Gnome;

class Program
{
    private readonly Adw.Application _application;
    private readonly IServiceProvider _serviceProvider;
    private readonly MainWindowController  _mainWindowController;
    private Views.MainWindow? _mainWindow;

    private readonly ApplicationInfo _applicationInfo = new()
    {
        Id = "org.bacmanni.jellyplayer",
        Developer = "Joni Bäckström",
        Email = "joni.j.backstrom@gmail.com",
        Name = "JellyPlayer",
        Version = "1.0.0-beta",
        Website = "https://github.com/bacmanni/jellyplayer",
        IssueUrl = "https://github.com/bacmanni/jellyplayer/issues/new",
        Icon = "jellyplayer-icon",
        ReleaseNotes = "<p>Initial release</p>",
        Artists = [ "Ruut Kiiskilä" ]
    };
    
    public static int Main(string[] args) => new Program().Run();
    private int Run()
    {
        try
        {
            return _application.RunWithSynchronizationContext([_applicationInfo.Id]);
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
        Gio.Module.Initialize();
        
        var serviceCollection = new ServiceCollection();
        ConfigureServices(_applicationInfo, serviceCollection);
        _serviceProvider = serviceCollection.BuildServiceProvider();
        
        var apiService = _serviceProvider.GetService<IJellyPlayerApiService>();
        var playerService = _serviceProvider.GetService<IPlayerService>();
        var fileService = _serviceProvider.GetService<IFileService>();
        var configurationService = _serviceProvider.GetService<IConfigurationService>();
        
        configurationService.Load();
        
        var sdkClientSettings = _serviceProvider.GetRequiredService<JellyfinSdkSettings>();
        sdkClientSettings.Initialize(
            _applicationInfo.Name,
            _applicationInfo.Version,
            "JellyPlayer Gnome",
            $"jellyplayer-{Guid.NewGuid():N}");
        
        var resourceFile = Path.GetFullPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)) + "/org.bacmanni.jellyplayer.gresource";
        Gio.Functions.ResourcesRegister(Gio.Functions.ResourceLoad(resourceFile));
        
        _mainWindowController = new MainWindowController(apiService, configurationService, playerService, fileService, _applicationInfo);
        
        _application = Adw.Application.New(_applicationInfo.Id, Gio.ApplicationFlags.NonUnique);
        _application.OnActivate += async (sender, args) =>
        {
            if (_mainWindow != null)
            {
                _mainWindow.Present();
                return;
            }
        
            _mainWindow = new Views.MainWindow((Adw.Application) sender, _mainWindowController, _application);
            _ = _mainWindow.StartAsync();
        };
        
        _application.OnShutdown += ApplicationOnOnShutdown;
    }

    private void ApplicationOnOnShutdown(Gio.Application sender, EventArgs args)
    {
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
                        applicationInfo.Name,
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
            serviceProvider => new ConfigurationService(fileSystem: serviceProvider.GetRequiredService<IFileSystem>(), applicationId: _applicationInfo.Id)
        );
        serviceCollection.AddSingleton<IFileSystem, FileSystem>();
        serviceCollection.AddSingleton<IJellyPlayerApiService, JellyPlayerApiService>();
        serviceCollection.AddSingleton<IPlayerService, PlayerService>();
        serviceCollection.AddSingleton<IFileService, FileService>();
        serviceCollection.AddSingleton<MainWindowController>();
    }
}