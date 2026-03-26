using ErrorOr;

namespace OVR.Modules.Reporting.Services;

public record ReportMetadata(
    string Rsc,
    string OrisCode,
    string? Discipline,
    int Version,
    string DataHash,
    DateTime GeneratedAt);

public record GeneratedReport(byte[] Pdf, ReportMetadata Metadata);

public interface IReportGenerator
{
    Task<ErrorOr<GeneratedReport>> GenerateAsync(
        string rsc, string orisCode, ReportDataOptions options, CancellationToken ct = default);
}
