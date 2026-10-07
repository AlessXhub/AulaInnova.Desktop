using System.Text;
using System.Text.Json;
using AulaInnova.Desktop.Models;

namespace AulaInnova.Desktop.Services;

public sealed class ApiAuthenticationService : IAuthenticationService
{
    private readonly ApiTransport _api;
    private readonly ApiSession _session;

    public ApiAuthenticationService(ApiTransport api, ApiSession session)
    {
        _api = api;
        _session = session;
    }

    public async Task<UserSession?> SignInAsync(string userName, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _api.PostAnonymousAsync<LoginResponse>(
                "api/Auth/login",
                new { usuario = userName.Trim(), password },
                cancellationToken);

            var claims = ReadClaims(response.Token);
            var roleName = claims.Role;
            var role = roleName switch
            {
                "Administrador" => UserRole.Administrador,
                "Profesor" or "Docente" => UserRole.Docente,
                "Auxiliar" => UserRole.Auxiliar,
                _ => throw new ApiException("Esta cuenta no tiene un rol habilitado para la aplicación de escritorio.")
            };

            _session.Set(response.Token, response.ExpiraUtc);
            return new UserSession(
                claims.UserId,
                claims.UserName,
                claims.UserName,
                role,
                roleName,
                PermissionsFor(role));
        }
        catch (ApiException exception) when (exception.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return null;
        }
    }

    private static (string UserId, string UserName, string Role) ReadClaims(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3) throw new ApiException("La API devolvió un token de acceso inválido.");
        var payload = parts[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        using var document = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
        var root = document.RootElement;

        var userId = GetString(root, "sub") ?? string.Empty;
        var userName = GetString(root, "unique_name")
            ?? GetString(root, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")
            ?? "usuario";
        var role = GetString(root, "role")
            ?? GetString(root, "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
            ?? string.Empty;
        return (userId, userName, role);
    }

    private static string? GetString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property)) return null;
        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Array => property.EnumerateArray().FirstOrDefault().GetString(),
            _ => null
        };
    }

    private static IReadOnlySet<string> PermissionsFor(UserRole role) => role switch
    {
        UserRole.Administrador => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "accounts.manage", "people.manage", "schedules.manage", "coverage.verify",
            "records.correct", "reports.view", "incidents.view", "incidents.create",
            "visual.view", "notices.view"
        },
        UserRole.Docente => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "responsibilities.view", "attendance.view", "movements.view", "incidents.view",
            "incidents.create", "class.context", "visual.view", "notices.view"
        },
        _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "responsibilities.view", "coverage.view", "coverage.verify", "presence.view",
            "attendance.view", "access.view", "movements.view", "incidents.view",
            "incidents.create", "contingencies.create", "records.view", "notices.view"
        }
    };

    private sealed record LoginResponse(string Token, DateTime ExpiraUtc);
}
