using System.Collections.ObjectModel;
using AulaInnova.Desktop.Infrastructure;
using AulaInnova.Desktop.Models;
using AulaInnova.Desktop.Services;

namespace AulaInnova.Desktop.ViewModels;

public sealed class RecordsPageViewModel : PageViewModel
{
    private readonly IAulaInnovaDataService _dataService;
    private readonly IDialogService _dialogService;
    private readonly UserSession _session;
    private readonly List<WorkspaceRecord> _allRecords = [];
    private string _searchText = string.Empty;
    private string _selectedFilter = "Todos";
    private bool _sortAscending = true;
    private string _sortProperty = "Primary";
    private int _currentPage = 1;
    private bool _isEditorOpen;
    private bool _isDetailsOpen;
    private bool _isEditing;
    private WorkspaceRecord? _selectedRecord;
    private string _feedbackMessage = string.Empty;

    public RecordsPageViewModel(
        IAulaInnovaDataService dataService,
        IDialogService dialogService,
        UserSession session,
        PageDefinition definition)
        : base(definition.Title, definition.Description, definition.RequirementRefs)
    {
        _dataService = dataService;
        _dialogService = dialogService;
        _session = session;
        Definition = definition;

        FilterOptions.Add("Todos");
        foreach (var filter in definition.FilterOptions ?? []) FilterOptions.Add(filter);

        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        SimulateErrorCommand = new AsyncRelayCommand(SimulateErrorAsync);
        SortCommand = new RelayCommand<string>(SortBy);
        NextPageCommand = new RelayCommand(NextPage, () => CurrentPage < TotalPages);
        PreviousPageCommand = new RelayCommand(PreviousPage, () => CurrentPage > 1);
        OpenCreateCommand = new RelayCommand(OpenCreate, () => Definition.CanCreate);
        OpenEditCommand = new RelayCommand<WorkspaceRecord>(OpenEdit, item => Definition.CanCreate && item is not null);
        OpenDetailsCommand = new RelayCommand<WorkspaceRecord>(OpenDetails, item => item is not null);
        CloseEditorCommand = new RelayCommand(CloseEditor);
        CloseDetailsCommand = new RelayCommand(() => IsDetailsOpen = false);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        DeactivateCommand = new AsyncRelayCommand(DeactivateAsync, () => SelectedRecord is not null && Definition.CanCreate);
    }

    public PageDefinition Definition { get; }
    public ObservableCollection<WorkspaceRecord> Records { get; } = [];
    public ObservableCollection<string> FilterOptions { get; } = [];
    public ObservableCollection<FormField> EditorFields { get; } = [];
    public int PageSize { get; } = 6;

    public string SearchText
    {
        get => _searchText;
        set { if (SetProperty(ref _searchText, value)) { CurrentPage = 1; ApplyView(); } }
    }

    public string SelectedFilter
    {
        get => _selectedFilter;
        set { if (SetProperty(ref _selectedFilter, value)) { CurrentPage = 1; ApplyView(); } }
    }

    public int CurrentPage
    {
        get => _currentPage;
        private set
        {
            if (SetProperty(ref _currentPage, value))
            {
                OnPropertyChanged(nameof(PageLabel));
                NextPageCommand.RaiseCanExecuteChanged();
                PreviousPageCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int TotalPages { get; private set; } = 1;
    public string PageLabel => $"Página {CurrentPage} de {TotalPages}";
    public int ResultCount { get; private set; }
    public bool HasRecords => Records.Count > 0;
    public bool IsEditorOpen { get => _isEditorOpen; private set => SetProperty(ref _isEditorOpen, value); }
    public bool IsDetailsOpen { get => _isDetailsOpen; private set => SetProperty(ref _isDetailsOpen, value); }
    public bool IsEditing { get => _isEditing; private set => SetProperty(ref _isEditing, value); }
    public string EditorTitle => IsEditing ? $"Editar {Definition.Title.ToLowerInvariant()}" : Definition.CreateLabel;
    public WorkspaceRecord? SelectedRecord
    {
        get => _selectedRecord;
        private set { if (SetProperty(ref _selectedRecord, value)) DeactivateCommand.RaiseCanExecuteChanged(); }
    }
    public string FeedbackMessage { get => _feedbackMessage; private set => SetProperty(ref _feedbackMessage, value); }

    public AsyncRelayCommand RefreshCommand { get; }
    public AsyncRelayCommand SimulateErrorCommand { get; }
    public RelayCommand<string> SortCommand { get; }
    public RelayCommand NextPageCommand { get; }
    public RelayCommand PreviousPageCommand { get; }
    public RelayCommand OpenCreateCommand { get; }
    public RelayCommand<WorkspaceRecord> OpenEditCommand { get; }
    public RelayCommand<WorkspaceRecord> OpenDetailsCommand { get; }
    public RelayCommand CloseEditorCommand { get; }
    public RelayCommand CloseDetailsCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }
    public AsyncRelayCommand DeactivateCommand { get; }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        SetError(string.Empty);
        FeedbackMessage = string.Empty;
        try
        {
            var rows = await _dataService.GetRecordsAsync(Definition.Key);
            _allRecords.Clear();
            _allRecords.AddRange(rows);
            ApplyView();
        }
        catch (Exception ex)
        {
            _allRecords.Clear();
            Records.Clear();
            SetError(ex.Message);
            OnPropertyChanged(nameof(HasRecords));
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SimulateErrorAsync()
    {
        _dataService.FailNextRequest(Definition.Key);
        await LoadAsync();
    }

    private void ApplyView()
    {
        IEnumerable<WorkspaceRecord> query = _allRecords;
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(item =>
                item.Primary.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.Secondary.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.Tertiary.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.Status.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (!SelectedFilter.Equals("Todos", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(item => item.Status.Contains(SelectedFilter, StringComparison.OrdinalIgnoreCase));
        }

        query = (_sortProperty, _sortAscending) switch
        {
            ("Secondary", true) => query.OrderBy(item => item.Secondary),
            ("Secondary", false) => query.OrderByDescending(item => item.Secondary),
            ("Status", true) => query.OrderBy(item => item.Status),
            ("Status", false) => query.OrderByDescending(item => item.Status),
            (_, true) => query.OrderBy(item => item.Primary),
            _ => query.OrderByDescending(item => item.Primary)
        };

        var filtered = query.ToList();
        ResultCount = filtered.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling(ResultCount / (double)PageSize));
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;

        Records.Clear();
        foreach (var item in filtered.Skip((CurrentPage - 1) * PageSize).Take(PageSize)) Records.Add(item);

        OnPropertyChanged(nameof(ResultCount));
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(PageLabel));
        OnPropertyChanged(nameof(HasRecords));
        NextPageCommand.RaiseCanExecuteChanged();
        PreviousPageCommand.RaiseCanExecuteChanged();
    }

    private void SortBy(string? property)
    {
        property ??= "Primary";
        if (_sortProperty == property) _sortAscending = !_sortAscending;
        else { _sortProperty = property; _sortAscending = true; }
        ApplyView();
    }

    private void NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; ApplyView(); } }
    private void PreviousPage() { if (CurrentPage > 1) { CurrentPage--; ApplyView(); } }

    private void OpenCreate()
    {
        SelectedRecord = null;
        IsEditing = false;
        BuildFields(null);
        FeedbackMessage = string.Empty;
        OnPropertyChanged(nameof(EditorTitle));
        IsEditorOpen = true;
    }

    private void OpenEdit(WorkspaceRecord? record)
    {
        if (record is null) return;
        SelectedRecord = record;
        IsEditing = true;
        BuildFields(record);
        FeedbackMessage = string.Empty;
        OnPropertyChanged(nameof(EditorTitle));
        IsEditorOpen = true;
    }

    private void OpenDetails(WorkspaceRecord? record)
    {
        if (record is null) return;
        SelectedRecord = record;
        IsDetailsOpen = true;
    }

    private void BuildFields(WorkspaceRecord? record)
    {
        EditorFields.Clear();
        var definitions = Definition.EditorFields ??
        [
            new EditorFieldDefinition("primary", Definition.PrimaryHeader, "Dato principal del registro"),
            new EditorFieldDefinition("secondary", Definition.SecondaryHeader, "Seleccione o escriba un valor autorizado"),
            new EditorFieldDefinition("tertiary", Definition.TertiaryHeader, "Contexto necesario para comprender el registro"),
            new EditorFieldDefinition("status", Definition.StatusHeader, "Estado explícito, nunca inferido por falta de lectura")
        ];

        foreach (var definition in definitions)
        {
            var value = record is null ? string.Empty : ReadRecordValue(record, definition.Key);
            EditorFields.Add(new FormField
            {
                Key = definition.Key,
                Label = definition.Label,
                Hint = definition.Hint,
                IsRequired = definition.IsRequired && !(IsEditing && definition.Key == "password"),
                IsReadOnly = IsEditing && definition.IsReadOnlyOnEdit,
                Value = value
            });
        }
    }

    private async Task SaveAsync()
    {
        if (!EditorFields.All(field => field.Validate()))
        {
            FeedbackMessage = "Revise los campos marcados antes de continuar.";
            return;
        }

        var values = EditorFields.ToDictionary(field => field.Key, field => field.Value.Trim(), StringComparer.OrdinalIgnoreCase);
        var record = new WorkspaceRecord
        {
            Id = SelectedRecord?.Id ?? Guid.NewGuid().ToString("N"),
            Primary = GetValue(values, "primary"),
            Secondary = GetValue(values, "secondary"),
            Tertiary = GetValue(values, "tertiary"),
            Status = GetValue(values, "status", SelectedRecord?.Status ?? "Activo"),
            Detail = SelectedRecord?.Detail ?? $"Registrado por {_session.DisplayName} mediante la API.",
            Values = values
        };

        try
        {
            var result = await _dataService.SaveRecordAsync(Definition.Key, record, IsEditing, _session.DisplayName);
            FeedbackMessage = result.Message;
            if (!result.Success) return;
            IsEditorOpen = false;
            await LoadAsync();
            FeedbackMessage = result.Message;
        }
        catch (Exception ex)
        {
            FeedbackMessage = ex.Message;
        }
    }

    private async Task DeactivateAsync()
    {
        if (SelectedRecord is null) return;
        if (!_dialogService.Confirm("Confirmar operación", $"¿Desea deshabilitar o anular lógicamente “{SelectedRecord.Primary}”? El historial se conservará.")) return;

        var result = await _dataService.DeactivateRecordAsync(Definition.Key, SelectedRecord.Id, _session.DisplayName);
        FeedbackMessage = result.Message;
        IsDetailsOpen = false;
        await LoadAsync();
        FeedbackMessage = result.Message;
    }

    private void CloseEditor()
    {
        IsEditorOpen = false;
        FeedbackMessage = string.Empty;
    }

    private static string ReadRecordValue(WorkspaceRecord record, string key)
    {
        if (record.Values.TryGetValue(key, out var value)) return value;
        return key switch
        {
            "primary" => record.Primary,
            "secondary" => record.Secondary,
            "tertiary" => record.Tertiary,
            "status" => record.Status,
            _ => string.Empty
        };
    }

    private static string GetValue(IReadOnlyDictionary<string, string> values, string key, string fallback = "") =>
        values.TryGetValue(key, out var value) ? value : fallback;
}
