using AulaInnova.Desktop.Models;

namespace AulaInnova.Desktop.Services;

public sealed class MockAuthenticationService : IAuthenticationService
{
    private static readonly IReadOnlyDictionary<string, UserSession> Users =
        new Dictionary<string, UserSession>(StringComparer.OrdinalIgnoreCase)
        {
            ["admin"] = new(
                "USR-001", "Andrea López", "admin", UserRole.Administrador, "Administración escolar",
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "accounts.manage", "people.manage", "credentials.manage", "stations.manage",
                    "schedules.manage", "coverage.verify", "records.correct", "reports.view",
                    "incidents.view", "incidents.create", "visual.view", "notices.view"
                }),
            ["docente"] = new(
                "USR-014", "Carlos Méndez", "docente", UserRole.Docente, "Desarrollo de Software",
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "responsibilities.view", "attendance.view", "movements.view", "incidents.view",
                    "incidents.create", "class.context", "visual.view", "notices.view"
                }),
            ["auxiliar"] = new(
                "USR-021", "Laura Pérez", "auxiliar", UserRole.Auxiliar, "Acompañamiento escolar",
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "responsibilities.view", "coverage.view", "coverage.verify", "presence.view",
                    "attendance.view", "access.view", "movements.view", "incidents.view",
                    "incidents.create", "contingencies.create", "records.view", "notices.view"
                })
        };

    public async Task<UserSession?> SignInAsync(string userName, string password, CancellationToken cancellationToken = default)
    {
        await Task.Delay(450, cancellationToken);
        if (!string.Equals(password, "Aula2026!", StringComparison.Ordinal))
        {
            return null;
        }

        return Users.TryGetValue(userName.Trim(), out var session) ? session : null;
    }
}
