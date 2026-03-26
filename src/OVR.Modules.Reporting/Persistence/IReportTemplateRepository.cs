using OVR.Modules.Reporting.Persistence.Documents;

namespace OVR.Modules.Reporting.Persistence;

public interface IReportTemplateRepository
{
    Task<ReportTemplateDocument?> FindAsync(string orisCode, string? discipline, CancellationToken ct);
    Task<IReadOnlyList<ReportTemplateDocument>> GetAllAsync(CancellationToken ct);
    Task UpsertAsync(ReportTemplateDocument doc, CancellationToken ct);
}
