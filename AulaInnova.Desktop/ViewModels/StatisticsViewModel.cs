using System.Collections.ObjectModel;
using AulaInnova.Desktop.Infrastructure;
using AulaInnova.Desktop.Models;
using AulaInnova.Desktop.Services;

namespace AulaInnova.Desktop.ViewModels;

public sealed class StatisticsViewModel : PageViewModel
{
    private readonly IAulaInnovaDataService _dataService;
    private string _indicator = "Asistencia";
    private string _population = "3° D Software";
    private string _period = "Jornada actual";
    private string _lastCalculated = string.Empty;

    public StatisticsViewModel(IAulaInnovaDataService dataService)
        : base("Estadísticas descriptivas", "Consulte totales, frecuencias y duraciones con población, período y denominador explícitos.", "RF-22")
    {
        _dataService = dataService;
        Indicators = ["Asistencia", "Movimientos", "Incidencias", "Cobertura"];
        Populations = ["3° D Software", "2° D Software", "1° D Software"];
        Periods = ["Jornada actual", "Últimos 7 días", "Mes actual"];
        CalculateCommand = new AsyncRelayCommand(CalculateAsync);
    }

    public IReadOnlyList<string> Indicators { get; }
    public IReadOnlyList<string> Populations { get; }
    public IReadOnlyList<string> Periods { get; }
    public ObservableCollection<MetricCard> Metrics { get; } = [];
    public ObservableCollection<ChartDatum> Distribution { get; } = [];
    public string Indicator { get => _indicator; set => SetProperty(ref _indicator, value); }
    public string Population { get => _population; set => SetProperty(ref _population, value); }
    public string Period { get => _period; set => SetProperty(ref _period, value); }
    public string LastCalculated { get => _lastCalculated; private set => SetProperty(ref _lastCalculated, value); }
    public AsyncRelayCommand CalculateCommand { get; }

    public override Task LoadAsync()
    {
        return CalculateAsync();
    }

    private async Task CalculateAsync()
    {
        Metrics.Clear();
        Distribution.Clear();

        if (Indicator == "Asistencia")
        {
            Metrics.Add(new MetricCard("Registros válidos", "27", $"Población: {Population}", "info", "\uE8D7"));
            Metrics.Add(new MetricCard("Puntualidad", "88.9 %", "24 de 27 registros válidos", "success", "\uE823"));
            Metrics.Add(new MetricCard("Pendientes", "1", "No se transforma en ausencia", "warning", "\uE9D5"));
            foreach (var datum in await _dataService.GetAttendanceDistributionAsync()) Distribution.Add(datum);
        }
        else if (Indicator == "Movimientos")
        {
            Metrics.Add(new MetricCard("Episodios válidos", "8", $"Período: {Period}", "info", "\uE8F1"));
            Metrics.Add(new MetricCard("Duración media", "7 min", "Solo horas fiables", "success", "\uE823"));
            Metrics.Add(new MetricCard("Por revisar", "1", "Dato inválido no equivale a cero", "warning", "\uE9D5"));
            Distribution.Add(new ChartDatum("Baño", 4, 208, "4 episodios"));
            Distribution.Add(new ChartDatum("Clínica", 2, 104, "2 episodios"));
            Distribution.Add(new ChartDatum("Administrativo", 1, 52, "1 episodio"));
            Distribution.Add(new ChartDatum("Otro", 1, 52, "1 episodio"));
        }
        else if (Indicator == "Incidencias")
        {
            Metrics.Add(new MetricCard("Registros vigentes", "4", "Observaciones no suman faltas", "info", "\uE7BA"));
            Metrics.Add(new MetricCard("Tipos concretos", "3", "Catálogo institucional", "success", "\uE8EC"));
            Metrics.Add(new MetricCard("Observaciones", "5", "Aportes con autoría y hora", "info", "\uE70B"));
            Distribution.Add(new ChartDatum("Leves", 2, 208, "2 de 4 incidencias"));
            Distribution.Add(new ChartDatum("Graves", 1, 104, "1 de 4 incidencias"));
            Distribution.Add(new ChartDatum("Muy graves", 1, 104, "1 de 4 incidencias"));
        }
        else
        {
            Metrics.Add(new MetricCard("Necesidades", "5", $"Período: {Period}", "info", "\uE7B3"));
            Metrics.Add(new MetricCard("Asignadas", "4", "No demuestra presencia", "success", "\uE8FB"));
            Metrics.Add(new MetricCard("Pendientes", "1", "Requiere seguimiento", "warning", "\uE9D5"));
            Distribution.Add(new ChartDatum("Asignadas", 4, 240, "4 necesidades con responsable"));
            Distribution.Add(new ChartDatum("Confirmadas", 3, 180, "3 presencias verificadas"));
            Distribution.Add(new ChartDatum("Pendientes", 1, 60, "1 necesidad sin asignar"));
        }

        LastCalculated = $"Calculado {DateTime.Now:dd/MM/yyyy HH:mm} · denominador y población visibles";
    }
}
