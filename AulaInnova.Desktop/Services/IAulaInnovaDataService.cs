using AulaInnova.Desktop.Models;

namespace AulaInnova.Desktop.Services;

public sealed record DataChangedEventArgs(string Key);

public interface IAulaInnovaDataService
{
    event EventHandler<DataChangedEventArgs>? DataChanged;
    Task<IReadOnlyList<WorkspaceRecord>> GetRecordsAsync(string key, CancellationToken cancellationToken = default);
    Task<SaveResult> SaveRecordAsync(string key, WorkspaceRecord record, bool isEdit, string actor, CancellationToken cancellationToken = default);
    Task<SaveResult> DeactivateRecordAsync(string key, string id, string actor, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScheduleEntry>> GetScheduleAsync(UserSession session, CancellationToken cancellationToken = default);
    Task<SaveResult> CreateAssignmentAsync(ScheduleEntry entry, string actor, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReportRow>> GenerateReportAsync(string reportType, string period, string scope, CancellationToken cancellationToken = default);
    Task<DashboardSnapshot> GetDashboardAsync(UserRole role, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChartDatum>> GetAttendanceDistributionAsync(CancellationToken cancellationToken = default);
    Task<VisualSnapshot> GetVisualAsync(CancellationToken cancellationToken = default);
    void FailNextRequest(string key);
}
