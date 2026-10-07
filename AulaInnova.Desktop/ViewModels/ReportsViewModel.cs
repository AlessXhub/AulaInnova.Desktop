using System.Collections.ObjectModel;
using AulaInnova.Desktop.Infrastructure;
using AulaInnova.Desktop.Models;
using AulaInnova.Desktop.Services;

namespace AulaInnova.Desktop.ViewModels;

public sealed class ReportsViewModel : PageViewModel
{
    private readonly IAulaInnovaDataService _dataService;
    private string _reportType = "Asistencia";
    private string _period = "Jornada actual";
    private string _scope = "3° D Software";
    private string _generatedAt = string.Empty;
    private string _feedbackMessage = "Seleccione los parámetros y genere el reporte.";

    public ReportsViewModel(IAulaInnovaDataService dataService)
        : base("Reportes de seguimiento", "Genere consultas internas con período, filtros y fecha de generación.", "RF-21")
    {
        _dataService = dataService;
        ReportTypes = ["Asistencia", "Movimientos", "Conducta", "Cuidos"];
        Periods = ["Jornada actual", "Últimos 7 días", "Mes actual", "Ciclo vigente"];
        Scopes = ["3° D Software", "2° D Software", "1° D Software", "Todas las áreas autorizadas"];
        GenerateCommand = new AsyncRelayCommand(GenerateAsync);
        SimulateErrorCommand = new AsyncRelayCommand(SimulateErrorAsync);
    }

    public IReadOnlyList<string> ReportTypes { get; }
    public IReadOnlyList<string> Periods { get; }
    public IReadOnlyList<string> Scopes { get; }
    public ObservableCollection<ReportRow> Rows { get; } = [];
    public string ReportType { get => _reportType; set => SetProperty(ref _reportType, value); }
    public string Period { get => _period; set => SetProperty(ref _period, value); }
    public string Scope { get => _scope; set => SetProperty(ref _scope, value); }
    public string GeneratedAt { get => _generatedAt; private set => SetProperty(ref _generatedAt, value); }
    public string FeedbackMessage { get => _feedbackMessage; private set => SetProperty(ref _feedbackMessage, value); }
    public bool HasRows => Rows.Count > 0;
    public AsyncRelayCommand GenerateCommand { get; }
    public AsyncRelayCommand SimulateErrorCommand { get; }

    public override async Task LoadAsync()
    {
        if (Rows.Count == 0) await GenerateAsync();
    }

    private async Task GenerateAsync()
    {
        IsLoading = true;
        SetError(string.Empty);
        try
        {
            var result = await _dataService.GenerateReportAsync(ReportType, Period, Scope);
            Rows.Clear();
            foreach (var row in result) Rows.Add(row);
            GeneratedAt = $"Generado el {DateTime.Now:dd/MM/yyyy} a las {DateTime.Now:HH:mm}";
            FeedbackMessage = "Reporte actualizado con datos persistidos por la API.";
            OnPropertyChanged(nameof(HasRows));
        }
        catch (Exception ex)
        {
            Rows.Clear();
            SetError(ex.Message);
            OnPropertyChanged(nameof(HasRows));
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SimulateErrorAsync()
    {
        _dataService.FailNextRequest("reports");
        await GenerateAsync();
    }
}
