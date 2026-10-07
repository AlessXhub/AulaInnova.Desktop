using System.Globalization;
using System.Net.Http;
using AulaInnova.Desktop.Models;

namespace AulaInnova.Desktop.Services;

public sealed class ApiAulaInnovaDataService : IAulaInnovaDataService
{
    private readonly ApiTransport _api;
    private string? _failNextKey;

    public ApiAulaInnovaDataService(ApiTransport api)
    {
        _api = api;
    }

    public event EventHandler<DataChangedEventArgs>? DataChanged;

    public async Task<IReadOnlyList<WorkspaceRecord>> GetRecordsAsync(string key, CancellationToken cancellationToken = default)
    {
        ThrowIfSimulatedFailure(key);
        var rows = await _api.GetAsync<List<WorkspaceRecordResponse>>(
            $"api/workspace/records/{Uri.EscapeDataString(key)}",
            cancellationToken);
        return rows.Select(MapRecord).ToList();
    }

    public async Task<SaveResult> SaveRecordAsync(
        string key,
        WorkspaceRecord record,
        bool isEdit,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ThrowIfSimulatedFailure(key);
        var id = Uri.EscapeDataString(record.Id);
        switch (key.ToLowerInvariant())
        {
            case "accounts":
                if (isEdit)
                {
                    await _api.PutAsync<UserResponse>($"api/admin/users/{id}", new
                    {
                        email = Value(record, "secondary"),
                        personaId = OptionalLongValue(record, "personaId", "Persona ID"),
                        rol = Value(record, "tertiary")
                    }, cancellationToken);
                }
                else
                {
                    await _api.PostAsync<UserResponse>("api/admin/users", new
                    {
                        usuario = Value(record, "primary"),
                        email = Value(record, "secondary"),
                        password = Value(record, "password"),
                        personaId = LongValue(record, "personaId", "Persona ID"),
                        rol = Value(record, "tertiary")
                    }, cancellationToken);
                }
                break;

            case "students":
            case "staff":
                if (isEdit)
                {
                    await _api.PutAsync<PersonResponse>($"api/admin/people/{id}", new
                    {
                        nombres = Value(record, "primary"),
                        apellidos = Value(record, "secondary")
                    }, cancellationToken);
                }
                else
                {
                    var path = key == "students" ? "api/admin/people/students" : "api/admin/people/staff";
                    await _api.PostAsync<PersonResponse>(path, new
                    {
                        nombres = Value(record, "primary"),
                        apellidos = Value(record, "secondary"),
                        codigo = Value(record, "tertiary")
                    }, cancellationToken);
                }
                break;

            case "classrooms":
                await SaveSimpleCatalogAsync<ClassroomResponse>(
                    isEdit ? HttpMethod.Put : HttpMethod.Post,
                    isEdit ? $"api/admin/classrooms/{id}" : "api/admin/classrooms",
                    new { codigo = Value(record, "primary"), nombre = Value(record, "secondary") },
                    cancellationToken);
                break;

            case "subjects":
                await SaveSimpleCatalogAsync<SubjectResponse>(
                    isEdit ? HttpMethod.Put : HttpMethod.Post,
                    isEdit ? $"api/admin/subjects/{id}" : "api/admin/subjects",
                    new { codigo = Value(record, "primary"), nombre = Value(record, "secondary") },
                    cancellationToken);
                break;

            case "groups":
                {
                    var request = new
                    {
                        ciclo = ShortValue(record, "primary", "Ciclo"),
                        grado = Value(record, "secondary"),
                        seccion = Value(record, "tertiary"),
                        desde = DateValue(record, "desde", "Vigente desde"),
                        hasta = DateValue(record, "hasta", "Vigente hasta")
                    };
                    if (isEdit) await _api.PutAsync<GroupResponse>($"api/admin/groups/{id}", request, cancellationToken);
                    else await _api.PostAsync<GroupResponse>("api/admin/groups", request, cancellationToken);
                    break;
                }

            case "school-days":
                {
                    var request = new
                    {
                        fechaLocal = DateValue(record, "primary", "Fecha local"),
                        zonaHoraria = Value(record, "secondary"),
                        inicioUtc = DateTimeValue(record, "inicioUtc", "Inicio UTC"),
                        finUtc = DateTimeValue(record, "finUtc", "Fin UTC"),
                        limitePuntualUtc = DateTimeValue(record, "limiteUtc", "Límite de puntualidad UTC")
                    };
                    if (isEdit)
                    {
                        await _api.PutAsync<SchoolDayResponse>($"api/admin/jornadas/{id}", new
                        {
                            request.zonaHoraria,
                            request.inicioUtc,
                            request.finUtc,
                            request.limitePuntualUtc
                        }, cancellationToken);
                    }
                    else await _api.PostAsync<SchoolDayResponse>("api/admin/jornadas", request, cancellationToken);
                    break;
                }

            case "enrollments":
                {
                    var request = new
                    {
                        estudianteId = LongValue(record, "estudianteId", "Estudiante ID"),
                        grupoId = LongValue(record, "grupoId", "Grupo ID"),
                        desde = DateValue(record, "desde", "Desde"),
                        hasta = OptionalDateValue(record, "hasta", "Hasta")
                    };
                    if (isEdit)
                    {
                        await _api.PutAsync<EnrollmentResponse>($"api/admin/matriculas/{id}", new
                        {
                            request.grupoId,
                            request.desde,
                            request.hasta
                        }, cancellationToken);
                    }
                    else await _api.PostAsync<EnrollmentResponse>("api/admin/matriculas", request, cancellationToken);
                    break;
                }

            default:
                return new SaveResult(false, "Este módulo se consulta desde la API, pero todavía no tiene un formulario de escritura compatible en el escritorio.");
        }

        RaiseChanged(key, "records");
        return new SaveResult(true, isEdit
            ? "Cambios guardados en la API y registrados en auditoría."
            : "Registro creado en la API.");
    }

    public async Task<SaveResult> DeactivateRecordAsync(
        string key,
        string id,
        string actor,
        CancellationToken cancellationToken = default)
    {
        var safeId = Uri.EscapeDataString(id);
        var path = key.ToLowerInvariant() switch
        {
            "accounts" => $"api/admin/users/{safeId}/status?activo=false",
            "students" or "staff" => $"api/admin/people/{safeId}/status?habilitada=false",
            "classrooms" => $"api/admin/classrooms/{safeId}/status?habilitada=false",
            "groups" => $"api/admin/groups/{safeId}/status?habilitado=false",
            "subjects" => $"api/admin/subjects/{safeId}/status?habilitada=false",
            "school-days" => $"api/admin/jornadas/{safeId}/status?estado=ANULADA",
            "enrollments" => $"api/admin/matriculas/{safeId}/status?anulada=true",
            _ => string.Empty
        };
        if (string.IsNullOrEmpty(path))
            return new SaveResult(false, "La API no admite anulación desde este módulo del escritorio.");

        await _api.SendPatchAsync(path, cancellationToken);
        RaiseChanged(key, "records");
        return new SaveResult(true, "El registro se deshabilitó o anuló lógicamente en la API.");
    }

    public async Task<IReadOnlyList<ScheduleEntry>> GetScheduleAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        ThrowIfSimulatedFailure("schedule");
        var rows = await _api.GetAsync<List<ScheduleEntryResponse>>("api/workspace/schedule", cancellationToken);
        return rows.Select(r => new ScheduleEntry(r.Id, r.Day, r.Time, r.Subject, r.Group, r.Classroom, r.Responsible, r.Kind, r.Status)).ToList();
    }

    public async Task<SaveResult> CreateAssignmentAsync(ScheduleEntry entry, string actor, CancellationToken cancellationToken = default)
    {
        var (startUtc, endUtc) = ParseSchoolInterval(entry.Time);
        await _api.PostAsync<AssignmentResponse>("api/admin/asignaciones", new
        {
            jornadaId = ParseId(entry.Day, "Jornada ID"),
            grupoId = ParseId(entry.Group, "Grupo ID"),
            horarioClaseId = (long?)null,
            personalId = ParseId(entry.Responsible, "Personal ID"),
            aulaId = ParseId(entry.Classroom, "Aula ID"),
            tipo = entry.Kind,
            inicioUtc = startUtc.ToString("O", CultureInfo.InvariantCulture),
            finUtc = endUtc.ToString("O", CultureInfo.InvariantCulture),
            actividad = entry.Subject
        }, cancellationToken);

        RaiseChanged("schedule", "coverage", "records");
        return new SaveResult(true, "Asignación creada en la API. La presencia continúa pendiente hasta una verificación humana.");
    }

    public async Task<IReadOnlyList<ReportRow>> GenerateReportAsync(string reportType, string period, string scope, CancellationToken cancellationToken = default)
    {
        ThrowIfSimulatedFailure("reports");
        var path = $"api/workspace/reports?reportType={Uri.EscapeDataString(reportType)}&period={Uri.EscapeDataString(period)}&scope={Uri.EscapeDataString(scope)}";
        var rows = await _api.GetAsync<List<ReportRowResponse>>(path, cancellationToken);
        return rows.Select(r => new ReportRow(r.Label, r.Value, r.Context, r.Status)).ToList();
    }

    public async Task<DashboardSnapshot> GetDashboardAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        var response = await _api.GetAsync<DashboardResponse>("api/workspace/dashboard", cancellationToken);
        return new DashboardSnapshot(
            response.Metrics.Select(MapMetric).ToList(),
            response.RecentActivity.Select(a => new ActivityItem(a.Title, a.Context, a.Time, a.Tone)).ToList(),
            response.AttendanceDistribution.Select(MapChart).ToList());
    }

    public async Task<IReadOnlyList<ChartDatum>> GetAttendanceDistributionAsync(CancellationToken cancellationToken = default)
    {
        var response = await _api.GetAsync<DashboardResponse>("api/workspace/dashboard", cancellationToken);
        return response.AttendanceDistribution.Select(MapChart).ToList();
    }

    public async Task<VisualSnapshot> GetVisualAsync(CancellationToken cancellationToken = default)
    {
        var response = await _api.GetAsync<VisualResponse>("api/workspace/visual", cancellationToken);
        return new VisualSnapshot(
            response.Metrics.Select(MapMetric).ToList(),
            response.MovementSeries.Select(MapChart).ToList(),
            response.FeedbackMessage);
    }

    public void FailNextRequest(string key) => _failNextKey = key;

    private async Task SaveSimpleCatalogAsync<T>(HttpMethod method, string path, object request, CancellationToken cancellationToken)
    {
        if (method == HttpMethod.Put) await _api.PutAsync<T>(path, request, cancellationToken);
        else await _api.PostAsync<T>(path, request, cancellationToken);
    }

    private void ThrowIfSimulatedFailure(string key)
    {
        if (!string.Equals(_failNextKey, key, StringComparison.OrdinalIgnoreCase)) return;
        _failNextKey = null;
        throw new ApiException("Fallo de conexión simulado. Use “Volver a intentar” para consultar nuevamente la API.");
    }

    private void RaiseChanged(params string[] keys)
    {
        foreach (var key in keys) DataChanged?.Invoke(this, new DataChangedEventArgs(key));
    }

    private static WorkspaceRecord MapRecord(WorkspaceRecordResponse row) => new()
    {
        Id = row.Id,
        Primary = row.Primary,
        Secondary = row.Secondary,
        Tertiary = row.Tertiary,
        Status = row.Status,
        Detail = row.Detail,
        UpdatedAt = row.UpdatedAt.ToLocalTime(),
        Values = row.Values is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(row.Values, StringComparer.OrdinalIgnoreCase)
    };

    private static MetricCard MapMetric(MetricResponse value) =>
        new(value.Label, value.Value, value.Context, value.Tone, value.Icon);

    private static ChartDatum MapChart(ChartResponse value) =>
        new(value.Label, value.Value, value.DisplayWidth, value.Description);

    private static string Value(WorkspaceRecord record, string key)
    {
        if (record.Values.TryGetValue(key, out var value)) return value.Trim();
        return key switch
        {
            "primary" => record.Primary.Trim(),
            "secondary" => record.Secondary.Trim(),
            "tertiary" => record.Tertiary.Trim(),
            "status" => record.Status.Trim(),
            _ => string.Empty
        };
    }

    private static long LongValue(WorkspaceRecord record, string key, string label) =>
        long.TryParse(Value(record, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new ApiException($"{label} debe ser un número entero.");

    private static short ShortValue(WorkspaceRecord record, string key, string label) =>
        short.TryParse(Value(record, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new ApiException($"{label} debe ser un número entero válido.");

    private static long? OptionalLongValue(WorkspaceRecord record, string key, string label)
    {
        var text = Value(record, key);
        return string.IsNullOrWhiteSpace(text) ? null : ParseId(text, label);
    }

    private static long ParseId(string text, string label) =>
        long.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value
            : throw new ApiException($"{label} debe ser un número entero mayor que cero.");

    private static (DateTime StartUtc, DateTime EndUtc) ParseSchoolInterval(string text)
    {
        var parts = text.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var times = parts.Length == 2
            ? parts[1].Split('-', 2, StringSplitOptions.TrimEntries)
            : [];
        if (parts.Length != 2 || times.Length != 2 ||
            !DateOnly.TryParseExact(parts[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
            !TimeOnly.TryParseExact(times[0], "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) ||
            !TimeOnly.TryParseExact(times[1], "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end) ||
            end <= start)
        {
            throw new ApiException("El intervalo debe usar el formato AAAA-MM-DD HH:mm-HH:mm y finalizar después de iniciar.");
        }

        var timeZoneId = OperatingSystem.IsWindows()
            ? "Central America Standard Time"
            : "America/El_Salvador";
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var startLocal = DateTime.SpecifyKind(date.ToDateTime(start), DateTimeKind.Unspecified);
        var endLocal = DateTime.SpecifyKind(date.ToDateTime(end), DateTimeKind.Unspecified);
        return (
            TimeZoneInfo.ConvertTimeToUtc(startLocal, timeZone),
            TimeZoneInfo.ConvertTimeToUtc(endLocal, timeZone));
    }

    private static string DateValue(WorkspaceRecord record, string key, string label) =>
        DateOnly.TryParse(Value(record, key), CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)
            ? value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : throw new ApiException($"{label} debe usar el formato AAAA-MM-DD.");

    private static string? OptionalDateValue(WorkspaceRecord record, string key, string label)
    {
        var text = Value(record, key);
        return string.IsNullOrWhiteSpace(text) ? null : DateValue(record, key, label);
    }

    private static string DateTimeValue(WorkspaceRecord record, string key, string label) =>
        DateTimeOffset.TryParse(Value(record, key), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var value)
            ? value.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)
            : throw new ApiException($"{label} debe usar un formato ISO válido, por ejemplo 2026-09-23T13:00:00Z.");

    private sealed record WorkspaceRecordResponse(string Id, string Primary, string Secondary, string Tertiary, string Status, string Detail, DateTime UpdatedAt, Dictionary<string, string>? Values);
    private sealed record ScheduleEntryResponse(string Id, string Day, string Time, string Subject, string Group, string Classroom, string Responsible, string Kind, string Status);
    private sealed record ReportRowResponse(string Label, string Value, string Context, string Status);
    private sealed record MetricResponse(string Label, string Value, string Context, string Tone, string Icon);
    private sealed record ActivityResponse(string Title, string Context, string Time, string Tone);
    private sealed record ChartResponse(string Label, double Value, double DisplayWidth, string Description);
    private sealed record DashboardResponse(List<MetricResponse> Metrics, List<ActivityResponse> RecentActivity, List<ChartResponse> AttendanceDistribution);
    private sealed record VisualResponse(List<MetricResponse> Metrics, List<ChartResponse> MovementSeries, string FeedbackMessage);
    private sealed record UserResponse(Guid Id, string Usuario, string Email, bool Activo, long? PersonaId, string? Rol);
    private sealed record PersonResponse(long Id, string TipoPersona, string Nombres, string Apellidos, string Codigo, bool Habilitada, Guid? UsuarioId);
    private sealed record ClassroomResponse(long Id, string Codigo, string Nombre, bool Habilitada);
    private sealed record SubjectResponse(long Id, string Codigo, string Nombre, bool Habilitada);
    private sealed record GroupResponse(long Id, short Ciclo, string Grado, string Seccion, DateOnly Desde, DateOnly Hasta, bool Habilitado);
    private sealed record SchoolDayResponse(long Id, DateOnly FechaLocal, string ZonaHoraria, DateTime InicioUtc, DateTime FinUtc, DateTime LimitePuntualUtc, string Estado);
    private sealed record EnrollmentResponse(long Id, long EstudianteId, long GrupoId, DateOnly Desde, DateOnly? Hasta, bool Anulada);
    private sealed record AssignmentResponse(long Id, long JornadaId, long GrupoId, long? HorarioClaseId, long PersonalId, long AulaId, string Tipo, DateTime InicioUtc, DateTime FinUtc, string Estado, long? SustituyeId, string? Actividad);
}
