using System.Collections.ObjectModel;
using AulaInnova.Desktop.Infrastructure;
using AulaInnova.Desktop.Models;
using AulaInnova.Desktop.Services;

namespace AulaInnova.Desktop.ViewModels;

public sealed class ScheduleViewModel : PageViewModel
{
    private readonly IAulaInnovaDataService _dataService;
    private readonly UserSession _session;
    private bool _isEditorOpen;
    private string _feedbackMessage = string.Empty;
    private string _day = string.Empty;
    private string _time = $"{DateTime.Today:yyyy-MM-dd} 10:40-11:25";
    private string _subject = "Cuido";
    private string _group = string.Empty;
    private string _classroom = string.Empty;
    private string _responsible = string.Empty;
    private string _assignmentType = "CUIDO";

    public ScheduleViewModel(IAulaInnovaDataService dataService, UserSession session)
        : base(
            session.Role == UserRole.Administrador ? "Horarios, cuidos y sustituciones" : "Mis responsabilidades",
            session.Role == UserRole.Administrador
                ? "Administre horarios versionados y asigne cobertura sin superponer responsabilidades."
                : "Consulte clases, cuidos, sustituciones y períodos libres de la jornada.",
            "RF-08, RF-09")
    {
        _dataService = dataService;
        _session = session;
        CanManage = session.Role == UserRole.Administrador;
        AssignmentTypes = ["CLASE", "CUIDO", "SUSTITUCION"];
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        OpenEditorCommand = new RelayCommand(() => { FeedbackMessage = string.Empty; IsEditorOpen = true; }, () => CanManage);
        CloseEditorCommand = new RelayCommand(() => IsEditorOpen = false);
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => CanManage);
        SimulateErrorCommand = new AsyncRelayCommand(SimulateErrorAsync);
    }

    public ObservableCollection<ScheduleEntry> Entries { get; } = [];
    public IReadOnlyList<string> AssignmentTypes { get; }
    public bool CanManage { get; }
    public bool IsEditorOpen { get => _isEditorOpen; private set => SetProperty(ref _isEditorOpen, value); }
    public string FeedbackMessage { get => _feedbackMessage; private set => SetProperty(ref _feedbackMessage, value); }
    public string Day { get => _day; set => SetProperty(ref _day, value); }
    public string Time { get => _time; set => SetProperty(ref _time, value); }
    public string Subject { get => _subject; set => SetProperty(ref _subject, value); }
    public string Group { get => _group; set => SetProperty(ref _group, value); }
    public string Classroom { get => _classroom; set => SetProperty(ref _classroom, value); }
    public string Responsible { get => _responsible; set => SetProperty(ref _responsible, value); }
    public string AssignmentType { get => _assignmentType; set => SetProperty(ref _assignmentType, value); }

    public AsyncRelayCommand RefreshCommand { get; }
    public RelayCommand OpenEditorCommand { get; }
    public RelayCommand CloseEditorCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }
    public AsyncRelayCommand SimulateErrorCommand { get; }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        SetError(string.Empty);
        try
        {
            var schedule = await _dataService.GetScheduleAsync(_session);
            Entries.Clear();
            foreach (var entry in schedule) Entries.Add(entry);
        }
        catch (Exception ex)
        {
            Entries.Clear();
            SetError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SaveAsync()
    {
        if (new[] { Day, Time, Subject, Group, Classroom, Responsible, AssignmentType }.Any(string.IsNullOrWhiteSpace))
        {
            FeedbackMessage = "Complete todos los campos antes de crear la asignación.";
            return;
        }

        if (!Time.Contains('-') || !Time.Contains(' '))
        {
            FeedbackMessage = "Use un intervalo con el formato AAAA-MM-DD HH:mm-HH:mm.";
            return;
        }

        var entry = new ScheduleEntry(
            Guid.NewGuid().ToString("N"), Day.Trim(), Time.Trim(), Subject.Trim(), Group.Trim(), Classroom.Trim(),
            Responsible.Trim(), AssignmentType.Trim(), "Asignado");
        var result = await _dataService.CreateAssignmentAsync(entry, _session.DisplayName);
        FeedbackMessage = result.Message;
        if (!result.Success) return;
        IsEditorOpen = false;
        await LoadAsync();
        FeedbackMessage = result.Message;
    }

    private async Task SimulateErrorAsync()
    {
        _dataService.FailNextRequest("schedule");
        await LoadAsync();
    }
}
