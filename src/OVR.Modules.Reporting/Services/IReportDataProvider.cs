using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.Reporting.Services;

public record ReportDataOptions(
    string Language,
    IReadOnlyDictionary<string, string>? ExtraParameters = null,
    CommunicationContent? Content = null,
    DateOnly? Date = null);

public record CommunicationContent(
    string Title,
    string? Subtitle,
    string? Description,
    string? Body = null,
    string? Affects = null);

public record HeaderData(
    string EventName,
    string PhaseName,
    string Date,
    string? VenueName,
    string? LogoUrl);

public record FooterLogo(string Url, string? AltText);

public record FooterData(
    string GeneratedAt,
    string? OfficialText,
    IReadOnlyList<FooterLogo> Logos);

public record ReportData(
    HeaderData Header,
    FooterData Footer,
    CommunicationContent Communication,
    object Body);

public interface IReportDataProvider
{
    string OrisCode { get; }
    string? DisciplineCode { get; }

    Task<ReportData> GetDataAsync(Rsc rsc, ReportDataOptions options, CancellationToken ct = default);
}
