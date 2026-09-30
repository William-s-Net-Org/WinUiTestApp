using HockeyRoster.Presentation;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace HockeyRoster.WinUI;

public sealed partial class MainWindow : Window
{
    public PlayersViewModel ViewModel { get; }
    public MainWindow(PlayersViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Root.DataContext = viewModel;
        AppWindow.Resize(new SizeInt32(1100, 850));
    }
    private async void OnLoaded(object sender, RoutedEventArgs e) => await ViewModel.LoadCommand.ExecuteAsync(null);
}
