using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace OneDriveBrowserApp;

public partial class App : Application
{
    public App()
    {
        Services = ConfigureServices();

        this.InitializeComponent();
    }

    public new static App Current => (App)Application.Current;

    public IServiceProvider Services { get; }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddHttpClient<IGraphClientWrapper, GraphClientWrapper>();
        services.AddSingleton<IGraphClientWrapper, GraphClientWrapper>();
        services.AddTransient<ILogFileWriter, LogFileWriter>();
        services.AddTransient<IFolderCache, FolderCache>();
        services.AddSingleton<IThumbnailCache, ThumbnailCache>();
        services.AddTransient<IFileMatcher, FileMatcher>();
        services.AddTransient<MainViewModel>();
        return services.BuildServiceProvider();
    }

}