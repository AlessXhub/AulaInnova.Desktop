using System.Collections.ObjectModel;
using AulaInnova.Desktop.Infrastructure;
using AulaInnova.Desktop.Models;
using AulaInnova.Desktop.Services;

namespace AulaInnova.Desktop.ViewModels;

public sealed class VisualAnalysisViewModel : PageViewModel
{
    private readonly IAulaInnovaDataService _dataService;
    private string _selectedSession = "Programación IV · 14/09/2026 · 10:40-11:25";
    private bool _hasValidData = true;
    private string _feedbackMessage = string.Empty;

    public VisualAnalysisViewModel(IAulaInnovaDataService dataService)
        : base(
            "Análisis visual descriptivo",
            "Revise únicamente indicadores agregados observables, su validez, cobertura y contexto declarado.",
            "RF-23, RF-24, RF-25, RF-26")
    {
        _dataService = dataService;
        Sessions =
        [
            "Programación IV · 14/09/2026 · 10:40-11:25",
            "Bases de Datos · 13/09/2026 · 08:10-08:55",
            "Lenguaje · 12/09/2026 · 09:10-09:55"
        ];
        SelectSessionCommand = new AsyncRelayCommand(LoadSessionAsync);
        SimulateUnavailableCommand = new RelayCommand(SimulateUnavailable);
        RestoreCommand = new AsyncRelayCommand(LoadSessionAsync);
    }

    public IReadOnlyList<string> Sessions { get; }
    public ObservableCollection<MetricCard> Metrics { get; } = [];
    public ObservableCollection<ChartDatum> MovementSeries { get; } = [];
    public string SelectedSession { get => _selectedSession; set => SetProperty(ref _selectedSession, value); }
    public bool HasValidData { get => _hasValidData; private set => SetProperty(ref _hasValidData, value); }
    public string FeedbackMessage { get => _feedbackMessage; private set => SetProperty(ref _feedbackMessage, value); }
    public AsyncRelayCommand SelectSessionCommand { get; }
    public RelayCommand SimulateUnavailableCommand { get; }
    public AsyncRelayCommand RestoreCommand { get; }

    public override Task LoadAsync() => LoadSessionAsync();

    private async Task LoadSessionAsync()
    {
        IsLoading = true;
        SetError(string.Empty);
        try
        {
            var snapshot = await _dataService.GetVisualAsync();
            Metrics.Clear();
            foreach (var metric in snapshot.Metrics) Metrics.Add(metric);
            MovementSeries.Clear();
            foreach (var datum in snapshot.MovementSeries) MovementSeries.Add(datum);
            HasValidData = snapshot.MovementSeries.Count > 0;
            FeedbackMessage = snapshot.FeedbackMessage;
        }
        catch (Exception ex)
        {
            Metrics.Clear();
            MovementSeries.Clear();
            HasValidData = false;
            SetError(ex.Message);
            FeedbackMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SimulateUnavailable()
    {
        HasValidData = false;
        Metrics.Clear();
        MovementSeries.Clear();
        FeedbackMessage = "No hay mediciones válidas para esta sesión. La falta de datos no se representa como cero ni modifica registros operativos.";
    }
}
