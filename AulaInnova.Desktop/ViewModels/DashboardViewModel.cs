using System.Collections.ObjectModel;
using AulaInnova.Desktop.Models;
using AulaInnova.Desktop.Services;

namespace AulaInnova.Desktop.ViewModels;

public sealed class DashboardViewModel : PageViewModel
{
    private readonly IAulaInnovaDataService _dataService;
    private readonly UserSession _session;

    public DashboardViewModel(IAulaInnovaDataService dataService, UserSession session)
        : base("Panel de inicio", $"Resumen operativo para {session.DisplayName}.", "RF-09, RF-12, RF-13, RF-27")
    {
        _dataService = dataService;
        _session = session;
    }

    public ObservableCollection<MetricCard> Metrics { get; } = [];
    public ObservableCollection<ActivityItem> RecentActivity { get; } = [];
    public ObservableCollection<ChartDatum> AttendanceDistribution { get; } = [];

    public override async Task LoadAsync()
    {
        IsLoading = true;
        SetError(string.Empty);
        try
        {
            var snapshot = await _dataService.GetDashboardAsync(_session.Role);
            Metrics.Clear();
            foreach (var metric in snapshot.Metrics) Metrics.Add(metric);
            RecentActivity.Clear();
            foreach (var item in snapshot.RecentActivity) RecentActivity.Add(item);
            AttendanceDistribution.Clear();
            foreach (var datum in snapshot.AttendanceDistribution) AttendanceDistribution.Add(datum);
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
