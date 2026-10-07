using AulaInnova.Desktop.Models;

namespace AulaInnova.Desktop.Services;

public interface IAuthenticationService
{
    Task<UserSession?> SignInAsync(string userName, string password, CancellationToken cancellationToken = default);
}
