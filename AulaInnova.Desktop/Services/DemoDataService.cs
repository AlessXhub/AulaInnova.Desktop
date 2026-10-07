using AulaInnova.Desktop.Models;

namespace AulaInnova.Desktop.Services;

public sealed class DemoDataService : IAulaInnovaDataService
{
    private readonly object _gate = new();
    private readonly Dictionary<string, List<WorkspaceRecord>> _records;
    private readonly List<ScheduleEntry> _schedule;
    private string? _failNextKey;

    public DemoDataService()
    {
        _records = SeedRecords();
        _schedule = SeedSchedule();
    }

    public event EventHandler<DataChangedEventArgs>? DataChanged;

    public async Task<IReadOnlyList<WorkspaceRecord>> GetRecordsAsync(string key, CancellationToken cancellationToken = default)
    {
        await DelayAndMaybeFailAsync(key, cancellationToken);
        lock (_gate)
        {
            return _records.TryGetValue(key, out var rows)
                ? rows.Select(Clone).ToList()
                : Array.Empty<WorkspaceRecord>();
        }
    }

    public async Task<SaveResult> SaveRecordAsync(
        string key,
        WorkspaceRecord record,
        bool isEdit,
        string actor,
        CancellationToken cancellationToken = default)
    {
        await DelayAndMaybeFailAsync(key, cancellationToken);
        SaveResult result;

        lock (_gate)
        {
            if (!_records.TryGetValue(key, out var rows))
            {
                rows = [];
                _records[key] = rows;
            }

            if (key == "incidents" && !isEdit)
            {
                var duplicate = rows.FirstOrDefault(item =>
                    item.Primary.Equals(record.Primary, StringComparison.OrdinalIgnoreCase) &&
                    item.Secondary.Equals(record.Secondary, StringComparison.OrdinalIgnoreCase) &&
                    !item.Status.Contains("ANUL", StringComparison.OrdinalIgnoreCase));

                if (duplicate is not null)
                {
                    duplicate.Detail = $"{duplicate.Detail}\n{DateTime.Now:HH:mm} · {actor}: {record.Tertiary}".Trim();
                    duplicate.UpdatedAt = DateTime.Now;
                    AddAudit(actor, "Observación añadida", $"{record.Primary} · {record.Secondary}", "Sin aumentar el conteo de incidencias.");
                    result = new SaveResult(true, "La incidencia ya existía. El aporte se añadió como observación con autoría y hora.", Clone(duplicate));
                    RaiseChanged(key, "records");
                    return result;
                }
            }

            if (key == "attendance" && !isEdit && rows.Any(item => item.Primary.Equals(record.Primary, StringComparison.OrdinalIgnoreCase)))
            {
                return new SaveResult(false, "Ya existe una asistencia para este estudiante y la jornada actual.");
            }

            if (isEdit)
            {
                var existing = rows.FirstOrDefault(item => item.Id == record.Id);
                if (existing is null)
                {
                    return new SaveResult(false, "El registro ya no está disponible. Actualice la vista e inténtelo de nuevo.");
                }

                var before = $"{existing.Primary} | {existing.Secondary} | {existing.Tertiary} | {existing.Status}";
                existing.Primary = record.Primary.Trim();
                existing.Secondary = record.Secondary.Trim();
                existing.Tertiary = record.Tertiary.Trim();
                existing.Status = record.Status.Trim();
                existing.Detail = record.Detail.Trim();
                existing.UpdatedAt = DateTime.Now;
                AddAudit(actor, "Corrección trazable", existing.Primary, $"Antes: {before}. Después: {existing.Primary} | {existing.Secondary} | {existing.Tertiary} | {existing.Status}.");
                result = new SaveResult(true, "Cambios guardados y registrados en auditoría.", Clone(existing));
            }
            else
            {
                var created = Clone(record);
                created.UpdatedAt = DateTime.Now;
                rows.Insert(0, created);
                AddAudit(actor, "Registro creado", created.Primary, $"Origen: operación manual simulada · Módulo: {key}.");
                result = new SaveResult(true, "Registro guardado en el entorno simulado.", Clone(created));
            }
        }

        RaiseChanged(key, "records");
        return result;
    }

    public async Task<SaveResult> DeactivateRecordAsync(
        string key,
        string id,
        string actor,
        CancellationToken cancellationToken = default)
    {
        await DelayAndMaybeFailAsync(key, cancellationToken);
        lock (_gate)
        {
            if (!_records.TryGetValue(key, out var rows))
            {
                return new SaveResult(false, "No se encontró el módulo solicitado.");
            }

            var record = rows.FirstOrDefault(item => item.Id == id);
            if (record is null)
            {
                return new SaveResult(false, "El registro ya no está disponible.");
            }

            var previous = record.Status;
            record.Status = key is "accounts" or "credentials" or "stations" ? "Deshabilitada" : "Anulado";
            record.UpdatedAt = DateTime.Now;
            AddAudit(actor, "Anulación lógica", record.Primary, $"Estado anterior: {previous}. Estado nuevo: {record.Status}.");
            RaiseChanged(key, "records");
            return new SaveResult(true, "La operación se aplicó de forma lógica y conserva el historial.", Clone(record));
        }
    }

    public async Task<IReadOnlyList<ScheduleEntry>> GetScheduleAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        await DelayAndMaybeFailAsync("schedule", cancellationToken);
        lock (_gate)
        {
            return session.Role == UserRole.Administrador
                ? _schedule.ToList()
                : _schedule.Where(item => item.Responsible.Equals(session.DisplayName, StringComparison.OrdinalIgnoreCase) || item.Kind == "Período libre").ToList();
        }
    }

    public async Task<SaveResult> CreateAssignmentAsync(ScheduleEntry entry, string actor, CancellationToken cancellationToken = default)
    {
        await DelayAndMaybeFailAsync("schedule", cancellationToken);
        lock (_gate)
        {
            var conflict = _schedule.FirstOrDefault(item =>
                item.Day.Equals(entry.Day, StringComparison.OrdinalIgnoreCase) &&
                item.Time.Equals(entry.Time, StringComparison.OrdinalIgnoreCase) &&
                item.Responsible.Equals(entry.Responsible, StringComparison.OrdinalIgnoreCase) &&
                item.Kind != "Período libre");

            if (conflict is not null)
            {
                return new SaveResult(false, $"Conflicto: {entry.Responsible} ya tiene {conflict.Kind.ToLowerInvariant()} en {conflict.Classroom} durante {conflict.Time}.");
            }

            _schedule.Add(entry);
            if (_records.TryGetValue("coverage", out var coverage))
            {
                coverage.Insert(0, NewRecord($"{entry.Classroom} · {entry.Time}", "Necesidad de cobertura", $"Asignada a {entry.Responsible}", "Confirmación pendiente", $"{entry.Kind} para {entry.Group}. La asignación no prueba presencia."));
            }
            AddAudit(actor, "Asignación creada", entry.Responsible, $"{entry.Kind} · {entry.Day} · {entry.Time} · {entry.Classroom}.");
        }

        RaiseChanged("schedule", "coverage", "records", "notices");
        return new SaveResult(true, "Asignación creada. La presencia seguirá pendiente hasta una verificación humana.");
    }

    public async Task<IReadOnlyList<ReportRow>> GenerateReportAsync(
        string reportType,
        string period,
        string scope,
        CancellationToken cancellationToken = default)
    {
        await DelayAndMaybeFailAsync("reports", cancellationToken);
        return reportType switch
        {
            "Asistencia" =>
            [
                new("Registros válidos", "27", $"{scope} · {period}", "Incluidos"),
                new("Llegadas puntuales", "24", "Hasta las 7:00:00 a. m.", "88.9 %"),
                new("Llegadas tardías", "3", "Después de las 7:00:00 a. m.", "11.1 %"),
                new("Pendientes de verificación", "1", "No se contabiliza como ausencia", "Revisión")
            ],
            "Movimientos" =>
            [
                new("Salidas aceptadas", "8", $"{scope} · {period}", "Válidas"),
                new("Regresos aceptados", "7", "Episodios cerrados", "Válidos"),
                new("Movimientos por revisar", "1", "Sin salida compatible", "Revisión"),
                new("Duración media", "7 min", "Solo episodios con horas fiables", "Calculada")
            ],
            "Conducta" =>
            [
                new("Incidencias vigentes", "4", $"{scope} · {period}", "Conteo único"),
                new("Leves", "2", "Por tipo concreto", "50 %"),
                new("Graves", "1", "Por tipo concreto", "25 %"),
                new("Muy graves", "1", "Por tipo concreto", "25 %")
            ],
            _ =>
            [
                new("Necesidades registradas", "5", $"{scope} · {period}", "Total"),
                new("Asignadas", "4", "Asignación no equivale a presencia", "80 %"),
                new("Presencia confirmada", "3", "Verificación humana", "60 %"),
                new("Pendientes", "1", "Requiere seguimiento", "Revisión")
            ]
        };
    }

    private IReadOnlyList<MetricCard> GetDashboardMetrics(UserRole role) => role switch
    {
        UserRole.Administrador =>
        [
            new("Asistencias registradas", "27", "Jornada actual · 24 puntuales", "success", "\uE8D7"),
            new("Coberturas pendientes", "2", "Ninguna se interpreta como ausencia", "warning", "\uE7B3"),
            new("Movimientos por revisar", "1", "Requiere validación humana", "warning", "\uE9D5"),
            new("Incidencias vigentes", "4", "Conteo único por tipo y jornada", "info", "\uE7BA")
        ],
        UserRole.Docente =>
        [
            new("Clase actual", "3° D", "Programación IV · Aula 12", "info", "\uE7BE"),
            new("Presentes registrados", "27", "Una asistencia pendiente de verificar", "success", "\uE716"),
            new("Salidas abiertas", "1", "Motivo declarado: clínica", "warning", "\uE8F1"),
            new("Sesión visual", "Disponible", "Solo indicadores agregados válidos", "info", "\uE9D9")
        ],
        _ =>
        [
            new("Responsabilidad actual", "Cuido", "Aula 8 · 10:40-11:25", "info", "\uE8D4"),
            new("Coberturas pendientes", "2", "Una dentro de su ámbito", "warning", "\uE7B3"),
            new("Movimientos por revisar", "1", "Regreso sin salida compatible", "warning", "\uE9D5"),
            new("Avisos internos", "3", "Actualizados hace 2 min", "info", "\uE7F4")
        ]
    };

    private IReadOnlyList<ActivityItem> GetRecentActivity(UserRole role) =>
    [
        new("Asistencia registrada", "María Fernanda Rivera · Aula 12", "10:42", "success"),
        new("Cobertura asignada", "Laura Pérez · Aula 8 · confirmación pendiente", "10:36", "warning"),
        new("Movimiento por revisar", "Regreso sin salida compatible · Aula 9", "10:18", "warning"),
        new("Incidencia actualizada", "Observación trazable añadida sin duplicar", "09:23", "info")
    ];

    private IReadOnlyList<ChartDatum> GetAttendanceDistribution() =>
    [
        new("Puntuales", 24, 300, "24 de 27 registros válidos"),
        new("Tardías", 3, 58, "3 de 27 registros válidos"),
        new("Pendientes", 1, 36, "No se contabiliza como ausencia")
    ];

    private IReadOnlyList<ChartDatum> GetVisualMovementSeries() =>
    [
        new("10:40", 18, 112, "18 unidades de movimiento observable"),
        new("10:45", 27, 168, "27 unidades de movimiento observable"),
        new("10:50", 21, 132, "21 unidades de movimiento observable"),
        new("10:55", 0, 0, "Intervalo no evaluable"),
        new("11:00", 34, 212, "34 unidades de movimiento observable"),
        new("11:05", 23, 144, "23 unidades de movimiento observable")
    ];

    public void FailNextRequest(string key) => _failNextKey = key;

    public Task<DashboardSnapshot> GetDashboardAsync(UserRole role, CancellationToken cancellationToken = default) =>
        Task.FromResult(new DashboardSnapshot(
            GetDashboardMetrics(role),
            GetRecentActivity(role),
            GetAttendanceDistribution()));

    public Task<IReadOnlyList<ChartDatum>> GetAttendanceDistributionAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(GetAttendanceDistribution());

    public Task<VisualSnapshot> GetVisualAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new VisualSnapshot(
            [
                new MetricCard("Personas visibles estimadas", "25.3", "Promedio · AULA_COMPLETA · personas", "info", "\uE716"),
                new MetricCard("Movimiento observable medio", "23.6", "Unidad demostrativa · método MV-0.3", "info", "\uE9D9"),
                new MetricCard("Cobertura válida", "83.3 %", "5 de 6 intervalos evaluables", "success", "\uE823"),
                new MetricCard("Variación", "5.4", "Desviación de muestras válidas", "info", "\uE9D2")
            ],
            GetVisualMovementSeries(),
            "Resultados de demostración. El procesamiento de cámara no está conectado."));

    private async Task DelayAndMaybeFailAsync(string key, CancellationToken cancellationToken)
    {
        await Task.Delay(220, cancellationToken);
        if (string.Equals(_failNextKey, key, StringComparison.OrdinalIgnoreCase))
        {
            _failNextKey = null;
            throw new InvalidOperationException("No fue posible cargar los datos simulados. Compruebe la conexión y vuelva a intentarlo.");
        }
    }

    private void AddAudit(string actor, string action, string target, string detail)
    {
        if (!_records.TryGetValue("records", out var audit))
        {
            return;
        }

        audit.Insert(0, NewRecord(action, target, actor, "Trazable", $"{DateTime.Now:dd/MM/yyyy HH:mm} · {detail}"));
    }

    private void RaiseChanged(params string[] keys)
    {
        foreach (var key in keys)
        {
            DataChanged?.Invoke(this, new DataChangedEventArgs(key));
        }
    }

    private static WorkspaceRecord Clone(WorkspaceRecord value) => new()
    {
        Id = value.Id,
        Primary = value.Primary,
        Secondary = value.Secondary,
        Tertiary = value.Tertiary,
        Status = value.Status,
        Detail = value.Detail,
        UpdatedAt = value.UpdatedAt
        ,
        Values = new Dictionary<string, string>(value.Values, StringComparer.OrdinalIgnoreCase)
    };

    private static WorkspaceRecord NewRecord(string primary, string secondary, string tertiary, string status, string detail) => new()
    {
        Primary = primary,
        Secondary = secondary,
        Tertiary = tertiary,
        Status = status,
        Detail = detail
    };

    private static Dictionary<string, List<WorkspaceRecord>> SeedRecords() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["accounts"] =
        [
            NewRecord("Andrea López", "admin@arrupe.edu.sv", "Administración escolar", "Activa", "Permisos explícitos: cuentas, auditoría, correcciones, reportes y configuración."),
            NewRecord("Carlos Méndez", "cmendez@arrupe.edu.sv", "Docente · Desarrollo de Software", "Activa", "Ámbito: 3° D Software, clases asignadas y resultados agregados autorizados."),
            NewRecord("Laura Pérez", "lperez@arrupe.edu.sv", "Auxiliar · Acompañamiento escolar", "Activa", "Ámbito: cobertura, contingencias e incidencias autorizadas."),
            NewRecord("Miguel Hernández", "mhernandez@arrupe.edu.sv", "Docente · Matemática", "Deshabilitada", "Cuenta deshabilitada el 12/09/2026; se conserva historial.")
        ],
        ["students"] =
        [
            NewRecord("María Fernanda Rivera", "AI-EST-2026-031", "3° D Software", "Activa", "Matrícula vigente."),
            NewRecord("Diego Alejandro Martínez", "AI-EST-2026-032", "3° D Software", "Activa", "Matrícula vigente."),
            NewRecord("Sofía Valentina Cruz", "AI-EST-2026-033", "3° D Software", "Activa", "Matrícula vigente."),
            NewRecord("José Andrés Portillo", "AI-EST-2026-034", "3° D Software", "Activa", "Matrícula vigente."),
            NewRecord("Camila Beatriz López", "AI-EST-2026-035", "3° D Software", "Activa", "Matrícula vigente.")
        ],
        ["staff"] =
        [
            NewRecord("Carlos Méndez", "Docente", "Desarrollo de Software", "Activo", "Responsable de Programación IV · 3° D Software."),
            NewRecord("Laura Pérez", "Auxiliar", "Acompañamiento escolar", "Activa", "Autorizada para contingencias y verificación de cobertura."),
            NewRecord("Rosa Elena Aguilar", "Docente", "Lenguaje y Literatura", "Activa", "Responsable de Lenguaje · 3° D Software."),
            NewRecord("Óscar Villalta", "Docente", "Matemática", "Activo", "Responsable de Matemática · 3° D Software.")
        ],
        ["classrooms"] =
        [
            NewRecord("3° D Software", "Aula 12", "Tutor: Carlos Méndez", "28 estudiantes", "Jornada matutina · vigencia desde 02/02/2026."),
            NewRecord("2° D Software", "Aula 9", "Tutor: Rosa Elena Aguilar", "30 estudiantes", "Jornada matutina · vigencia desde 02/02/2026."),
            NewRecord("1° D Software", "Aula 6", "Tutor: Óscar Villalta", "31 estudiantes", "Jornada matutina · vigencia desde 02/02/2026.")
        ],
        ["coverage"] =
        [
            NewRecord("Aula 8 · 10:40-11:25", "Necesidad de cobertura", "Asignada a Laura Pérez", "Confirmación pendiente", "La asignación no prueba presencia. Última lectura compatible: pendiente."),
            NewRecord("Aula 9 · 11:25-12:10", "Necesidad de cobertura", "Sin responsable asignado", "Pendiente", "Requiere asignación sin superposición de responsabilidades."),
            NewRecord("Aula 12 · 08:10-08:55", "Sustitución", "Óscar Villalta", "Presencia registrada", "Verificación humana registrada a las 08:12."),
            NewRecord("Aula 6 · 07:25-08:10", "Clase regular", "Rosa Elena Aguilar", "Confirmada", "Presencia verificada por personal autorizado.")
        ],
        ["responsibilities"] =
        [
            NewRecord("07:25-08:10", "Programación IV", "3° D Software · Aula 12", "Completada", "Clase establecida en horario vigente."),
            NewRecord("08:10-08:55", "Período libre", "Sin aula asignada", "Informativo", "No equivale a aceptar un cuido ni a disponibilidad confirmada."),
            NewRecord("10:40-11:25", "Cuido", "2° D Software · Aula 9", "Asignado", "La presencia se confirma por separado."),
            NewRecord("11:25-12:10", "Bases de Datos", "3° D Software · Laboratorio 2", "Siguiente", "Clase establecida en horario vigente.")
        ],
        ["presence"] =
        [
            NewRecord("Carlos Méndez", "Aula 12 · Programación IV", "Llegada 07:23 · salida 08:11", "Registrada", "Carnet NFC-0042 · configuración EST-AULA-3DS-01 v4."),
            NewRecord("Laura Pérez", "Aula 8 · cuido", "Asignación 10:40 · sin lectura", "Confirmación pendiente", "La falta de lectura no prueba ausencia."),
            NewRecord("Óscar Villalta", "Aula 12 · sustitución", "Llegada 08:12", "Registrada", "Compatible con asignación vigente."),
            NewRecord("Rosa Elena Aguilar", "Aula 9", "Lectura 09:02 sin asignación compatible", "Por revisar", "Evento conservado para revisión; no alterna automáticamente su estado.")
        ],
        ["attendance"] =
        [
            NewRecord("María Fernanda Rivera", "06:48:12", "Aula 12 · estación v4", "Puntual", "Primera lectura válida de la jornada."),
            NewRecord("Diego Alejandro Martínez", "07:00:00", "Aula 12 · estación v4", "Puntual", "El límite de puntualidad incluye las 7:00:00 a. m."),
            NewRecord("Sofía Valentina Cruz", "07:00:01", "Aula 12 · estación v4", "Tardía", "Clasificación de llegada al salón, no de ingreso al colegio."),
            NewRecord("José Andrés Portillo", "Sin lectura confirmada", "Aula 12 · estación v4", "Pendiente de verificación", "No se presenta como ausencia hasta verificación humana."),
            NewRecord("Camila Beatriz López", "07:18:44", "Contingencia manual", "Justificada", "Hora del hecho y de incorporación conservadas por separado.")
        ],
        ["access"] =
        [
            NewRecord("María Fernanda Rivera", "Ingreso al colegio", "Acceso principal · 06:37", "Aceptado", "Operación institucional separada de la asistencia del salón."),
            NewRecord("Diego Alejandro Martínez", "Salida ordinaria", "Acceso principal · 15:12", "Aceptado", "Evento explícito de salida institucional."),
            NewRecord("Sofía Valentina Cruz", "Salida anticipada", "Acceso principal · 11:24", "Confirmada", "Confirmada por personal autorizado; seleccionar salida no concede permiso.")
        ],
        ["movements"] =
        [
            NewRecord("María Fernanda Rivera", "Salida temporal · Baño", "09:21-09:28 · Aula 12", "Aceptado", "Selección consumida tras la lectura; episodio cerrado en 7 min."),
            NewRecord("Diego Alejandro Martínez", "Salida temporal · Clínica", "10:14 · Aula 12", "Aceptado", "Salida abierta. El motivo aporta contexto y no certifica permiso."),
            NewRecord("Sofía Valentina Cruz", "Regreso al aula", "10:18 · Aula 9", "Por revisar", "No existe salida abierta compatible; no se inventó un episodio previo."),
            NewRecord("José Andrés Portillo", "Salida temporal · Baño", "10:22 · Aula 12", "Repetido", "La lectura repetida no creó una segunda salida.")
        ],
        ["incidents"] =
        [
            NewRecord("Diego Alejandro Martínez", "Uso inadecuado del equipo", "Descripción confirmada por el docente", "Leve", "14/09/2026 09:35 · Autoría conservada · una observación adicional."),
            NewRecord("José Andrés Portillo", "Irrespeto verbal", "Descripción confirmada por auxiliar", "Grave", "13/09/2026 11:04 · Sin sanción automática."),
            NewRecord("Sofía Valentina Cruz", "Daño deliberado", "Descripción confirmada por docente", "Muy grave", "11/09/2026 08:42 · Requiere seguimiento institucional humano.")
        ],
        ["contingencies"] =
        [
            NewRecord("Camila Beatriz López", "Asistencia manual", "Hecho 07:18 · incorporado 07:26", "Registrado", "Motivo: carnet en reemplazo · autora Laura Pérez."),
            NewRecord("Carlos Méndez", "Presencia manual", "Hecho 08:12 · incorporado 08:14", "Registrado", "Motivo: estación en mantenimiento · origen operador humano."),
            NewRecord("María Fernanda Rivera", "Regreso manual", "Hecho 09:28 · incorporado 09:31", "Registrado", "Motivo: falla de lectura · no duplica eventos pendientes del dispositivo.")
        ],
        ["records"] =
        [
            NewRecord("Corrección trazable", "Asistencia · Camila Beatriz López", "Andrea López", "Trazable", "14/09/2026 09:02 · Antes: tardía. Después: justificada. Motivo conservado."),
            NewRecord("Anulación lógica", "Movimiento MOV-00917", "Andrea López", "Trazable", "14/09/2026 08:46 · Registro anulado sin borrar evidencia."),
            NewRecord("Publicación de observación", "Incidencia INC-00142", "Andrea López", "Trazable", "13/09/2026 14:10 · Publicación comunicable sin exponer autor en portal estudiantil.")
        ],
        ["notices"] =
        [
            NewRecord("Cobertura pendiente", "Aula 9 · 11:25-12:10", "Sin responsable asignado", "Pendiente", "Actualizado 10:38 · no representa ausencia confirmada."),
            NewRecord("Movimiento inconsistente", "Sofía Valentina Cruz · Aula 9", "Regreso sin salida compatible", "Por revisar", "Actualizado 10:18 · abrir el registro relacionado."),
            NewRecord("Estación sin comunicación", "EST-AULA-2DS-02", "Última señal 09:18", "Sin comunicación", "Actualizado 09:23 · revisar red y cola local; no deducir ausencia."),
            NewRecord("Cobertura asignada", "Aula 8 · 10:40-11:25", "Laura Pérez", "Confirmación pendiente", "Asignación relacionada CUI-00231.")
        ],
        ["contexts"] =
        [
            NewRecord("Programación IV · 3° D", "10:40-11:25 · Aula 12", "Práctica guiada de estructuras de datos", "Registrado", "Contexto declarado por Carlos Méndez; no modifica las mediciones."),
            NewRecord("Bases de Datos · 3° D", "08:10-08:55 · Laboratorio 2", "Ejercicio colaborativo de normalización", "Registrado", "Contexto declarado por el docente responsable.")
        ]
    };

    private static List<ScheduleEntry> SeedSchedule() =>
    [
        new("SCH-001", "Lunes", "07:25-08:10", "Programación IV", "3° D Software", "Aula 12", "Carlos Méndez", "Clase", "Vigente"),
        new("SCH-002", "Lunes", "08:10-08:55", "Período libre", "-", "-", "Carlos Méndez", "Período libre", "Informativo"),
        new("SCH-003", "Lunes", "09:10-09:55", "Lenguaje", "3° D Software", "Aula 12", "Rosa Elena Aguilar", "Clase", "Vigente"),
        new("SCH-004", "Lunes", "09:55-10:40", "Matemática", "3° D Software", "Aula 12", "Óscar Villalta", "Clase", "Vigente"),
        new("SCH-005", "Lunes", "10:40-11:25", "Cuido", "2° D Software", "Aula 9", "Laura Pérez", "Cuido", "Asignado"),
        new("SCH-006", "Lunes", "11:25-12:10", "Bases de Datos", "3° D Software", "Laboratorio 2", "Carlos Méndez", "Clase", "Siguiente")
    ];
}
