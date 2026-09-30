using HockeyRoster.Application;
using HockeyRoster.Infrastructure;
using HockeyRoster.Presentation;
using Microsoft.UI.Xaml;

namespace HockeyRoster.WinUI;

public partial class App : Microsoft.UI.Xaml.Application
{
    private Window? _window;
    public App() => InitializeComponent();
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HockeyRoster", "players.json");
        var service = new PlayerService(new JsonPlayerRepository(path));
        _window = new MainWindow(new PlayersViewModel(service));
        _window.Activate();
    }
}
