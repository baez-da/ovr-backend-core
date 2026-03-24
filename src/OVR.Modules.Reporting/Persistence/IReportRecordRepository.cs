using OVR.Modules.Reporting.Persistence.Documents;

namespace OVR.Modules.Reporting.Persistence;

public interface IReportRecordRepository
{
    Task<ReportRecordDocument?> GetLatestAsync(string rsc, string orisCode, CancellationToken ct);
    Task AddAsync(ReportRecordDocument doc, CancellationToken ct);
}
