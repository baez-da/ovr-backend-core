using OVR.Modules.Reporting.Persistence.Documents;

namespace OVR.Modules.Reporting.Persistence;

public interface IReportLayoutRepository
{
    Task<ReportLayoutDocument?> FindAsync(string component, string? discipline, CancellationToken ct);
    Task UpsertAsync(ReportLayoutDocument doc, CancellationToken ct);
}
