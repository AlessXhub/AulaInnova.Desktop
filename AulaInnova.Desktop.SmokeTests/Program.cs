using AulaInnova.Desktop.Models;
using AulaInnova.Desktop.Services;

var checks = new List<(string Name, Func<Task> Run)>
{
    ("Autenticación por rol", VerifyAuthenticationAsync),
    ("Asistencia única por jornada", VerifyAttendanceUniquenessAsync),
    ("Incidencia repetida como observación", VerifyIncidentObservationAsync),
    ("Conflictos y asignaciones de horario", VerifyScheduleRulesAsync),
    ("Anulación lógica con auditoría", VerifyLogicalDeactivationAsync),
    ("Falla simulada recuperable", VerifyRecoverableFailureAsync)
};

if (args.Contains("--api", StringComparer.OrdinalIgnoreCase))
{
    checks.Add(("Integración real API/Identity", VerifyApiIntegrationAsync));
}

var failures = new List<string>();
foreach (var check in checks)
{
    try
    {
        await check.Run();
        Console.WriteLine($"PASS  {check.Name}");
    }
    catch (Exception exception)
    {
        failures.Add($"{check.Name}: {exception.Message}");
        Console.WriteLine($"FAIL  {check.Name} - {exception.Message}");
    }
}

if (failures.Count > 0)
{
    Console.Error.WriteLine($"{failures.Count} prueba(s) fallaron.");
    Environment.ExitCode = 1;
    return;
}

Console.WriteLine($"Todas las pruebas pasaron ({checks.Count}/{checks.Count}).");

static async Task VerifyAuthenticationAsync()
{
    var authentication = new MockAuthenticationService();
    var admin = await authentication.SignInAsync("admin", "Aula2026!");
    var teacher = await authentication.SignInAsync("docente", "Aula2026!");
    var auxiliary = await authentication.SignInAsync("auxiliar", "Aula2026!");
    var denied = await authentication.SignInAsync("admin", "incorrecta");

    Require(admin?.Role == UserRole.Administrador, "La cuenta admin no obtuvo el rol esperado.");
    Require(teacher?.Role == UserRole.Docente, "La cuenta docente no obtuvo el rol esperado.");
    Require(auxiliary?.Role == UserRole.Auxiliar, "La cuenta auxiliar no obtuvo el rol esperado.");
    Require(denied is null, "Una contraseña inválida fue aceptada.");
}

static async Task VerifyAttendanceUniquenessAsync()
{
    var service = new DemoDataService();
    var before = await service.GetRecordsAsync("attendance");
    var duplicate = new WorkspaceRecord
    {
        Primary = "María Fernanda Rivera",
        Secondary = "07:02:00",
        Tertiary = "Contingencia manual",
        Status = "Tardía",
        Detail = "Intento duplicado de prueba."
    };

    var result = await service.SaveRecordAsync("attendance", duplicate, false, "admin");
    var after = await service.GetRecordsAsync("attendance");
    Require(!result.Success, "Se aceptó una segunda asistencia en la misma jornada.");
    Require(after.Count == before.Count, "El duplicado alteró el número de asistencias.");
}

static async Task VerifyIncidentObservationAsync()
{
    var service = new DemoDataService();
    var before = await service.GetRecordsAsync("incidents");
    var repeated = new WorkspaceRecord
    {
        Primary = "Diego Alejandro Martínez",
        Secondary = "Uso inadecuado del equipo",
        Tertiary = "Observación adicional confirmada",
        Status = "Leve",
        Detail = "Aporte de prueba."
    };

    var result = await service.SaveRecordAsync("incidents", repeated, false, "admin");
    var after = await service.GetRecordsAsync("incidents");
    Require(result.Success, "No se pudo incorporar la observación repetida.");
    Require(after.Count == before.Count, "La observación incrementó indebidamente el conteo de incidencias.");
    Require(after.Single(item => item.Id == before[0].Id).Detail.Contains("Observación adicional confirmada"), "La observación no quedó trazada.");
}

static async Task VerifyScheduleRulesAsync()
{
    var service = new DemoDataService();
    var conflict = new ScheduleEntry("TEST-1", "Lunes", "07:25-08:10", "Sustitución", "2° D", "Aula 9", "Carlos Méndez", "Sustitución", "Pendiente");
    var available = new ScheduleEntry("TEST-2", "Martes", "12:10-12:55", "Cuido", "1° D", "Aula 6", "Carlos Méndez", "Cuido", "Pendiente");

    var rejected = await service.CreateAssignmentAsync(conflict, "admin");
    var accepted = await service.CreateAssignmentAsync(available, "admin");
    var schedule = await service.GetScheduleAsync(AdminSession());
    var coverage = await service.GetRecordsAsync("coverage");

    Require(!rejected.Success, "Se aceptó una responsabilidad superpuesta.");
    Require(accepted.Success, "Se rechazó una asignación sin conflicto.");
    Require(schedule.Any(item => item.Id == "TEST-2"), "La asignación válida no apareció en el horario.");
    Require(coverage.Any(item => item.Primary.Contains("Aula 6") && item.Status.Contains("Confirmación")), "La asignación no continuó en cobertura.");
}

static async Task VerifyLogicalDeactivationAsync()
{
    var service = new DemoDataService();
    var accounts = await service.GetRecordsAsync("accounts");
    var target = accounts.First(item => item.Status == "Activa");
    var result = await service.DeactivateRecordAsync("accounts", target.Id, "admin");
    var updated = await service.GetRecordsAsync("accounts");
    var audit = await service.GetRecordsAsync("records");

    Require(result.Success, "La deshabilitación lógica falló.");
    Require(updated.Single(item => item.Id == target.Id).Status == "Deshabilitada", "La cuenta no cambió a deshabilitada.");
    Require(audit.Any(item => item.Primary == "Anulación lógica" && item.Secondary == target.Primary), "No se creó la trazabilidad de la deshabilitación.");
}

static async Task VerifyRecoverableFailureAsync()
{
    var service = new DemoDataService();
    service.FailNextRequest("students");
    var failed = false;
    try
    {
        _ = await service.GetRecordsAsync("students");
    }
    catch (InvalidOperationException)
    {
        failed = true;
    }

    var recovered = await service.GetRecordsAsync("students");
    Require(failed, "No se produjo la falla de demostración solicitada.");
    Require(recovered.Count > 0, "El servicio no se recuperó tras la falla simulada.");
}

static UserSession AdminSession() => new(
    "TEST-ADMIN",
    "Administrador de prueba",
    "admin",
    UserRole.Administrador,
    "Pruebas",
    new HashSet<string>());

static async Task VerifyApiIntegrationAsync()
{
    var password = Environment.GetEnvironmentVariable("AULAINNOVA_TEST_PASSWORD");
    Require(!string.IsNullOrWhiteSpace(password), "Defina AULAINNOVA_TEST_PASSWORD para ejecutar la prueba de integración.");

    var apiSession = new ApiSession();
    using var transport = new ApiTransport(apiSession);
    var authentication = new ApiAuthenticationService(transport, apiSession);
    var data = new ApiAulaInnovaDataService(transport);

    var session = await authentication.SignInAsync("admin", password!);
    Require(session?.Role == UserRole.Administrador, "La API no devolvió una sesión Administrador válida.");

    var accounts = await data.GetRecordsAsync("accounts");
    Require(accounts.Any(row => row.Primary.Equals("admin", StringComparison.OrdinalIgnoreCase)), "La cuenta bootstrap no apareció en la consulta del escritorio.");

    var dashboard = await data.GetDashboardAsync(UserRole.Administrador);
    Require(dashboard.Metrics.Count >= 4, "El panel conectado no devolvió sus métricas.");

    var report = await data.GenerateReportAsync("Asistencia", "Jornada actual", "Todas las áreas autorizadas");
    Require(report.Count >= 4, "El reporte conectado no devolvió filas.");

    _ = await data.GetScheduleAsync(session!);
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
