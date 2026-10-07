using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using AulaInnova.Desktop.Infrastructure;
using AulaInnova.Desktop.Models;
using AulaInnova.Desktop.Services;

namespace AulaInnova.Desktop.ViewModels;

public sealed class ShellViewModel : ObservableObject
{
    private readonly IAulaInnovaDataService _dataService;
    private readonly IDialogService _dialogService;
    private readonly Action _logout;
    private PageViewModel _currentPage = null!;
    private bool _isCompactNavigation;

    public ShellViewModel(
        UserSession session,
        IAulaInnovaDataService dataService,
        IDialogService dialogService,
        Action logout)
    {
        Session = session;
        _dataService = dataService;
        _dialogService = dialogService;
        _logout = logout;
        Navigation = BuildNavigation();
        NavigationView = CollectionViewSource.GetDefaultView(Navigation);
        NavigationView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(NavigationItem.Section)));

        NavigateCommand = new RelayCommand<NavigationItem>(Navigate);
        LogoutCommand = new RelayCommand(Logout);
        ToggleNavigationCommand = new RelayCommand(() => IsCompactNavigation = !IsCompactNavigation);

        var first = Navigation[0];
        first.IsSelected = true;
        CurrentPage = first.Page;
        _ = CurrentPage.LoadAsync();
    }

    public UserSession Session { get; }
    public ObservableCollection<NavigationItem> Navigation { get; }
    public ICollectionView NavigationView { get; }
    public string DisplayName => Session.DisplayName;
    public string RoleLabel => Session.Role.ToString();
    public string UserInitials => string.Join(string.Empty, Session.DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part => part[0])).ToUpperInvariant();
    public int AlertCount => 3;

    public PageViewModel CurrentPage { get => _currentPage; private set => SetProperty(ref _currentPage, value); }
    public bool IsCompactNavigation { get => _isCompactNavigation; set => SetProperty(ref _isCompactNavigation, value); }
    public RelayCommand<NavigationItem> NavigateCommand { get; }
    public RelayCommand LogoutCommand { get; }
    public RelayCommand ToggleNavigationCommand { get; }

    private ObservableCollection<NavigationItem> BuildNavigation()
    {
        var items = new ObservableCollection<NavigationItem>();
        Add(items, "Inicio", "Panel de inicio", "\uE80F", new DashboardViewModel(_dataService, Session));

        if (Session.Role == UserRole.Administrador)
        {
            Add(items, "Administración", "Usuarios y permisos", "\uE77B", Records(Definitions.Accounts));
            Add(items, "Administración", "Estudiantes", "\uE716", Records(Definitions.Students));
            Add(items, "Administración", "Personal", "\uE902", Records(Definitions.Staff));
            Add(items, "Administración", "Aulas", "\uE7BE", Records(Definitions.Classrooms));
            Add(items, "Administración", "Grupos", "\uE8B7", Records(Definitions.Groups));
            Add(items, "Administración", "Asignaturas", "\uE82D", Records(Definitions.Subjects));
            Add(items, "Administración", "Jornadas", "\uE787", Records(Definitions.SchoolDays));
            Add(items, "Administración", "Matrículas", "\uE8A5", Records(Definitions.Enrollments));
            Add(items, "Operación", "Horarios y cuidos", "\uE787", new ScheduleViewModel(_dataService, Session));
            Add(items, "Operación", "Cobertura de aulas", "\uE7B3", Records(Definitions.Coverage));
            Add(items, "Operación", "Presencia del personal", "\uE8D4", Records(Definitions.Presence));
            Add(items, "Operación", "Asistencias", "\uE8D7", Records(Definitions.Attendance));
            Add(items, "Operación", "Accesos al colegio", "\uE8F1", Records(Definitions.Access));
            Add(items, "Operación", "Salidas y regresos", "\uE9D5", Records(Definitions.Movements));
            Add(items, "Seguimiento", "Incidencias", "\uE7BA", Records(Definitions.Incidents));
            Add(items, "Seguimiento", "Contingencias", "\uE814", Records(Definitions.Contingencies));
            Add(items, "Seguimiento", "Historial y auditoría", "\uE81C", Records(Definitions.Records));
            Add(items, "Análisis", "Reportes", "\uE9D2", new ReportsViewModel(_dataService));
            Add(items, "Análisis", "Estadísticas", "\uE9D9", new StatisticsViewModel(_dataService));
            Add(items, "Análisis", "Contexto de clase", "\uE70B", Records(Definitions.Contexts));
            Add(items, "Análisis", "Análisis visual", "\uE9F9", new VisualAnalysisViewModel(_dataService));
            Add(items, "Seguimiento", "Avisos internos", "\uE7F4", Records(Definitions.Notices));
        }
        else if (Session.Role == UserRole.Docente)
        {
            Add(items, "Mi jornada", "Mis responsabilidades", "\uE787", new ScheduleViewModel(_dataService, Session));
            Add(items, "Mi jornada", "Asistencias", "\uE8D7", Records(Definitions.Attendance with { CanCreate = false }));
            Add(items, "Mi jornada", "Salidas y regresos", "\uE9D5", Records(Definitions.Movements with { CanCreate = false }));
            Add(items, "Seguimiento", "Incidencias", "\uE7BA", Records(Definitions.Incidents));
            Add(items, "Seguimiento", "Contexto de clase", "\uE70B", Records(Definitions.Contexts));
            Add(items, "Análisis", "Análisis visual", "\uE9F9", new VisualAnalysisViewModel(_dataService));
            Add(items, "Seguimiento", "Avisos internos", "\uE7F4", Records(Definitions.Notices with { CanCreate = false }));
        }
        else
        {
            Add(items, "Mi jornada", "Mis responsabilidades", "\uE787", new ScheduleViewModel(_dataService, Session));
            Add(items, "Operación", "Cobertura de aulas", "\uE7B3", Records(Definitions.Coverage));
            Add(items, "Operación", "Presencia del personal", "\uE8D4", Records(Definitions.Presence with { CanCreate = false }));
            Add(items, "Operación", "Asistencias", "\uE8D7", Records(Definitions.Attendance with { CanCreate = false }));
            Add(items, "Operación", "Accesos al colegio", "\uE8F1", Records(Definitions.Access with { CanCreate = false }));
            Add(items, "Operación", "Salidas y regresos", "\uE9D5", Records(Definitions.Movements with { CanCreate = false }));
            Add(items, "Seguimiento", "Incidencias", "\uE7BA", Records(Definitions.Incidents));
            Add(items, "Seguimiento", "Contingencias", "\uE814", Records(Definitions.Contingencies));
            Add(items, "Seguimiento", "Consulta institucional", "\uE721", Records(Definitions.Records with { Title = "Consulta institucional", CanCreate = false }));
            Add(items, "Análisis", "Reportes", "\uE9D2", new ReportsViewModel(_dataService));
            Add(items, "Seguimiento", "Avisos internos", "\uE7F4", Records(Definitions.Notices with { CanCreate = false }));
        }

        Add(items, "Cuenta", "Mi perfil", "\uE77B", new ProfileViewModel(Session));
        return items;
    }

    private RecordsPageViewModel Records(PageDefinition definition) => new(_dataService, _dialogService, Session, definition);

    private static void Add(ObservableCollection<NavigationItem> items, string section, string title, string icon, PageViewModel page) =>
        items.Add(new NavigationItem { Section = section, Title = title, Icon = icon, Page = page });

    private async void Navigate(NavigationItem? item)
    {
        if (item is null || item.Page == CurrentPage) return;
        foreach (var candidate in Navigation) candidate.IsSelected = candidate == item;
        CurrentPage = item.Page;
        await CurrentPage.LoadAsync();
    }

    private void Logout()
    {
        if (_dialogService.Confirm("Cerrar sesión", "¿Desea cerrar la sesión actual?")) _logout();
    }

    private static class Definitions
    {
        public static PageDefinition Accounts => new(
            "accounts", "Usuarios y permisos", "Administre las cuentas Identity persistidas por la API.",
            "Usuario", "Correo", "Rol", "Estado", "Crear cuenta", true,
            "No hay cuentas que coincidan con la búsqueda.", "RF-01, RF-02, RNF-04, RNF-05",
            ["Activa", "Deshabilitada"],
            [
                new("primary", "Usuario", "Nombre único de acceso.", IsReadOnlyOnEdit: true),
                new("secondary", "Correo", "Correo asociado a Identity."),
                new("tertiary", "Rol", "Administrador, Profesor, Auxiliar o Estudiante."),
                new("personaId", "Persona ID", "Identificador numérico de la persona habilitada; el administrador bootstrap puede dejarlo vacío.", false),
                new("password", "Contraseña inicial", "Obligatoria al crear; no se muestra ni se reemplaza al editar.")
            ]);

        public static PageDefinition Students => PersonDefinition(
            "students", "Estudiantes", "Registrar estudiante", "RF-03");

        public static PageDefinition Staff => PersonDefinition(
            "staff", "Personal", "Registrar personal", "RF-02, RF-03");

        public static PageDefinition Classrooms => new(
            "classrooms", "Aulas", "Administre el catálogo estable de aulas institucionales.",
            "Código", "Nombre", "Tipo", "Estado", "Crear aula", true,
            "No hay aulas registradas.", "RF-03, RF-08", ["Habilitada", "Deshabilitada"],
            [
                new("primary", "Código", "Código único del aula."),
                new("secondary", "Nombre", "Nombre visible del aula.")
            ]);

        public static PageDefinition Groups => new(
            "groups", "Grupos", "Administre grado, sección y vigencia sin mezclar grupos con aulas.",
            "Ciclo", "Grado", "Sección", "Estado", "Crear grupo", true,
            "No hay grupos registrados.", "RF-03, RF-08", ["Habilitado", "Deshabilitado"],
            [
                new("primary", "Ciclo", "Año/ciclo numérico, por ejemplo 2026."),
                new("secondary", "Grado", "Grado institucional."),
                new("tertiary", "Sección", "Sección del grupo."),
                new("desde", "Vigente desde", "Formato AAAA-MM-DD."),
                new("hasta", "Vigente hasta", "Formato AAAA-MM-DD.")
            ]);

        public static PageDefinition Subjects => new(
            "subjects", "Asignaturas", "Administre el catálogo de asignaturas utilizado por los horarios.",
            "Código", "Nombre", "Tipo", "Estado", "Crear asignatura", true,
            "No hay asignaturas registradas.", "RF-08", ["Habilitada", "Deshabilitada"],
            [
                new("primary", "Código", "Código único de la asignatura."),
                new("secondary", "Nombre", "Nombre institucional.")
            ]);

        public static PageDefinition SchoolDays => new(
            "school-days", "Jornadas", "Administre fechas, intervalos UTC y límite de puntualidad.",
            "Fecha local", "Zona horaria", "Intervalo", "Estado", "Crear jornada", true,
            "No hay jornadas registradas.", "RF-09, RF-13", ["PROGRAMADA", "ABIERTA", "CERRADA", "ANULADA"],
            [
                new("primary", "Fecha local", "Formato AAAA-MM-DD.", IsReadOnlyOnEdit: true),
                new("secondary", "Zona horaria", "Identificador aceptado por la API."),
                new("inicioUtc", "Inicio UTC", "Ejemplo: 2026-09-23T12:00:00Z."),
                new("finUtc", "Fin UTC", "Ejemplo: 2026-09-23T22:00:00Z."),
                new("limiteUtc", "Límite puntual UTC", "Ejemplo: 2026-09-23T13:00:00Z.")
            ]);

        public static PageDefinition Enrollments => new(
            "enrollments", "Matrículas", "Vincule estudiantes y grupos por identificadores de la API.",
            "Estudiante", "Grupo", "Vigencia", "Estado", "Crear matrícula", true,
            "No hay matrículas registradas.", "RF-03", ["Vigente", "Anulada"],
            [
                new("estudianteId", "Estudiante ID", "Identificador numérico de la persona estudiante.", IsReadOnlyOnEdit: true),
                new("grupoId", "Grupo ID", "Identificador numérico del grupo."),
                new("desde", "Desde", "Formato AAAA-MM-DD."),
                new("hasta", "Hasta", "Formato AAAA-MM-DD; opcional.", false)
            ]);

        public static PageDefinition Coverage => ReadOnly("coverage", "Cobertura de aulas", "Distinga necesidad, asignación, presencia y verificación.", "Intervalo", "Motivo", "Responsable", "Estado", "No hay necesidades de cobertura.", "RF-08, RF-12", ["PENDIENTE", "ASIGNADA", "CERRADA"]);
        public static PageDefinition Presence => ReadOnly("presence", "Presencia del personal", "Consulte responsabilidades y verificaciones humanas de presencia.", "Personal", "Responsabilidad", "Verificación", "Estado", "No hay registros de presencia.", "RF-11");
        public static PageDefinition Attendance => ReadOnly("attendance", "Asistencia diaria", "Consulte la llegada persistida; una lectura faltante no prueba ausencia.", "Estudiante", "Primera llegada", "Origen", "Clasificación", "No hay asistencias registradas.", "RF-13", ["PUNTUAL", "TARDIA", "PENDIENTE"]);
        public static PageDefinition Access => ReadOnly("access", "Accesos al colegio", "Revise accesos institucionales separados de los movimientos de aula.", "Persona", "Operación", "Hora", "Estado", "No hay accesos institucionales registrados.", "RF-10");
        public static PageDefinition Movements => ReadOnly("movements", "Salidas y regresos", "Revise salidas temporales y regresos registrados.", "Estudiante", "Motivo", "Intervalo y aula", "Estado", "No hay movimientos registrados.", "RF-14, RF-15");
        public static PageDefinition Incidents => ReadOnly("incidents", "Incidencias y observaciones", "Consulte hechos confirmados y observaciones sin sanción automática.", "Estudiante", "Tipo concreto", "Observaciones", "Categoría", "No hay incidencias registradas.", "RF-16, RNF-09");
        public static PageDefinition Contingencies => ReadOnly("contingencies", "Registro manual y contingencias", "Consulte eventos manuales con hora de hecho, recepción y autoría.", "Persona", "Evento", "Tiempos", "Estado", "No hay contingencias registradas.", "RF-17");
        public static PageDefinition Records => ReadOnly("records", "Historial y auditoría", "Consulte operación, registro, autor, hora y motivo de cada cambio.", "Operación", "Registro", "Autor", "Estado", "No hay entradas de auditoría.", "RF-18, RF-20, RNF-10", ["Trazable"]);
        public static PageDefinition Contexts => ReadOnly("contexts", "Contexto de clase", "Consulte descripciones asociadas a clases e intervalos.", "Clase", "Intervalo", "Actividad", "Estado", "No hay contextos registrados.", "RF-23", ["Registrado"]);
        public static PageDefinition Notices => ReadOnly("notices", "Avisos internos", "Este contrato aún no existe en la API; la vista permanece vacía hasta definirlo.", "Causa", "Contexto", "Relación", "Estado", "La API todavía no expone avisos internos.", "RF-27");

        private static PageDefinition PersonDefinition(string key, string title, string createLabel, string requirements) => new(
            key, title, "Administre identidades estables sin reescribir el historial.",
            "Nombres", "Apellidos", "Código", "Estado", createLabel, true,
            $"No hay {title.ToLowerInvariant()} que coincidan con los filtros.", requirements,
            ["Activa", "Deshabilitada"],
            [
                new("primary", "Nombres", "Nombres de la persona."),
                new("secondary", "Apellidos", "Apellidos de la persona."),
                new("tertiary", "Código", "Código institucional único.", IsReadOnlyOnEdit: true)
            ]);

        private static PageDefinition ReadOnly(
            string key, string title, string description,
            string primary, string secondary, string tertiary, string status,
            string empty, string requirements, IReadOnlyList<string>? filters = null) =>
            new(key, title, description, primary, secondary, tertiary, status,
                "", false, empty, requirements, filters);
    }
}
