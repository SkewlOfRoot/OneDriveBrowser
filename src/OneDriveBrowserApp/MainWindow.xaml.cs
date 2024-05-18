using MahApps.Metro.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace OneDriveBrowserApp;

public partial class MainWindow : MetroWindow
{
    public MainWindow()
    {
        DataContext = App.Current.Services.GetService<MainViewModel>();
        InitializeComponent();
    }

    private async void MainWindow_OnInitialized(object? sender, EventArgs e)
    {
        var viewModel = (MainViewModel) DataContext;
        await viewModel.Initialize();
    }
}