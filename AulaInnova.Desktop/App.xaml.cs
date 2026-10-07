using System.Windows;
using AulaInnova.Desktop.Infrastructure;
using AulaInnova.Desktop.Services;
using AulaInnova.Desktop.ViewModels;

namespace AulaInnova.Desktop;

public partial class App : Application
{
    private ApiTransport? _apiTransport;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var apiSession = new ApiSession();
        _apiTransport = new ApiTransport(apiSession);
        var dataService = new ApiAulaInnovaDataService(_apiTransport);
        var dialogService = new WpfDialogService();
        var authenticationService = new ApiAuthenticationService(_apiTransport, apiSession);
        var rootViewModel = new RootViewModel(authenticationService, dataService, dialogService, apiSession);

        var window = new MainWindow { DataContext = rootViewModel };
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _apiTransport?.Dispose();
        base.OnExit(e);
    }
}
