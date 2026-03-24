using OVR.Modules.Reporting.Persistence.Documents;

namespace OVR.Modules.Reporting.Persistence;

public interface IReportPartialRepository
{
    Task<IReadOnlyList<ReportPartialDocument>> GetAllAsync(CancellationToken ct);
    Task UpsertAsync(ReportPartialDocument doc, CancellationToken ct);
}
