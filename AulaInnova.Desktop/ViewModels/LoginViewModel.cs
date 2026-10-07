using AulaInnova.Desktop.Infrastructure;
using AulaInnova.Desktop.Models;
using AulaInnova.Desktop.Services;
using System.Net.Http;

namespace AulaInnova.Desktop.ViewModels;

public sealed class LoginViewModel : ObservableObject
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IDialogService _dialogService;
    private readonly Action<UserSession> _onAuthenticated;
    private string _userName = "admin";
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;

    public LoginViewModel(
        IAuthenticationService authenticationService,
        IDialogService dialogService,
        Action<UserSession> onAuthenticated)
    {
        _authenticationService = authenticationService;
        _dialogService = dialogService;
        _onAuthenticated = onAuthenticated;
        SignInCommand = new AsyncRelayCommand(SignInAsync, CanSignIn);
        ForgotPasswordCommand = new RelayCommand(() => _dialogService.ShowInfo(
            "Recuperación no disponible",
            "La API todavía no expone un flujo de recuperación de contraseña. Solicite el restablecimiento al administrador del sistema."));
    }

    public string UserName
    {
        get => _userName;
        set { if (SetProperty(ref _userName, value)) SignInCommand.RaiseCanExecuteChanged(); }
    }

    public string Password
    {
        get => _password;
        set { if (SetProperty(ref _password, value)) SignInCommand.RaiseCanExecuteChanged(); }
    }

    public string ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }
    public bool IsBusy { get => _isBusy; private set { if (SetProperty(ref _isBusy, value)) SignInCommand.RaiseCanExecuteChanged(); } }
    public AsyncRelayCommand SignInCommand { get; }
    public RelayCommand ForgotPasswordCommand { get; }

    private bool CanSignIn() => !IsBusy && !string.IsNullOrWhiteSpace(UserName) && !string.IsNullOrWhiteSpace(Password);

    private async Task SignInAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var session = await _authenticationService.SignInAsync(UserName, Password);
            if (session is null)
            {
                ErrorMessage = "No pudimos iniciar sesión. Revise el usuario, la contraseña o el estado de la cuenta.";
                return;
            }

            _onAuthenticated(session);
        }
        catch (ApiException exception)
        {
            ErrorMessage = exception.Message;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "No fue posible conectar con la API en http://localhost:5077. Iníciela y vuelva a intentarlo.";
        }
        finally
        {
            IsBusy = false;
        }
    }

}
