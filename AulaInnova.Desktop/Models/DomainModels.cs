using AulaInnova.Desktop.Infrastructure;

namespace AulaInnova.Desktop.Models;

public enum UserRole
{
    Administrador,
    Docente,
    Auxiliar
}

public sealed record UserSession(
    string Id,
    string DisplayName,
    string UserName,
    UserRole Role,
    string Area,
    IReadOnlySet<string> Permissions);

public sealed record DashboardSnapshot(
    IReadOnlyList<MetricCard> Metrics,
    IReadOnlyList<ActivityItem> RecentActivity,
    IReadOnlyList<ChartDatum> AttendanceDistribution);

public sealed record VisualSnapshot(
    IReadOnlyList<MetricCard> Metrics,
    IReadOnlyList<ChartDatum> MovementSeries,
    string FeedbackMessage);

public sealed class WorkspaceRecord : ObservableObject
{
    private string _primary = string.Empty;
    private string _secondary = string.Empty;
    private string _tertiary = string.Empty;
    private string _status = string.Empty;
    private string _detail = string.Empty;

    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Primary { get => _primary; set => SetProperty(ref _primary, value); }
    public string Secondary { get => _secondary; set => SetProperty(ref _secondary, value); }
    public string Tertiary { get => _tertiary; set => SetProperty(ref _tertiary, value); }
    public string Status { get => _status; set => SetProperty(ref _status, value); }
    public string Detail { get => _detail; set => SetProperty(ref _detail, value); }
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public Dictionary<string, string> Values { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed record SaveResult(bool Success, string Message, WorkspaceRecord? Record = null);
public sealed record MetricCard(string Label, string Value, string Context, string Tone, string Icon);
public sealed record ActivityItem(string Title, string Context, string Time, string Tone);
public sealed record ChartDatum(string Label, double Value, double DisplayWidth, string Description);

public sealed record ScheduleEntry(
    string Id,
    string Day,
    string Time,
    string Subject,
    string Group,
    string Classroom,
    string Responsible,
    string Kind,
    string Status);

public sealed record ReportRow(string Label, string Value, string Context, string Status);

public sealed class FormField : ObservableObject
{
    private string _value = string.Empty;
    private string _error = string.Empty;

    public required string Key { get; init; }
    public required string Label { get; init; }
    public string Hint { get; init; } = string.Empty;
    public bool IsRequired { get; init; } = true;
    public bool IsReadOnly { get; init; }
    public string Value { get => _value; set { if (SetProperty(ref _value, value)) Validate(); } }
    public string Error { get => _error; private set => SetProperty(ref _error, value); }
    public bool IsValid => string.IsNullOrEmpty(Error);

    public bool Validate()
    {
        Error = IsRequired && string.IsNullOrWhiteSpace(Value) ? $"{Label} es obligatorio." : string.Empty;
        OnPropertyChanged(nameof(IsValid));
        return IsValid;
    }
}

public sealed record EditorFieldDefinition(
    string Key,
    string Label,
    string Hint,
    bool IsRequired = true,
    bool IsReadOnlyOnEdit = false);

public sealed record PageDefinition(
    string Key,
    string Title,
    string Description,
    string PrimaryHeader,
    string SecondaryHeader,
    string TertiaryHeader,
    string StatusHeader,
    string CreateLabel,
    bool CanCreate,
    string EmptyMessage,
    string RequirementRefs,
    IReadOnlyList<string>? FilterOptions = null,
    IReadOnlyList<EditorFieldDefinition>? EditorFields = null);
