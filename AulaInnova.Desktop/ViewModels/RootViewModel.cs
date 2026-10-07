using AulaInnova.Desktop.Infrastructure;
using AulaInnova.Desktop.Models;
using AulaInnova.Desktop.Services;

namespace AulaInnova.Desktop.ViewModels;

public sealed class RootViewModel : ObservableObject
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IAulaInnovaDataService _dataService;
    private readonly IDialogService _dialogService;
    private readonly ApiSession _apiSession;
    private object _currentView = null!;

    public RootViewModel(
        IAuthenticationService authenticationService,
        IAulaInnovaDataService dataService,
        IDialogService dialogService,
        ApiSession apiSession)
    {
        _authenticationService = authenticationService;
        _dataService = dataService;
        _dialogService = dialogService;
        _apiSession = apiSession;
        ShowLogin();
    }

    public object CurrentView { get => _currentView; private set => SetProperty(ref _currentView, value); }

    private void ShowLogin()
    {
        _apiSession.Clear();
        CurrentView = new LoginViewModel(_authenticationService, _dialogService, OnAuthenticated);
    }

    private void OnAuthenticated(UserSession session) =>
        CurrentView = new ShellViewModel(session, _dataService, _dialogService, ShowLogin);
}
