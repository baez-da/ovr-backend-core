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
    string? LogoImageData,
    string? PictogramImageData,
    string? VenueEn,
    string? VenueLocal,
    string? DateStr,
    string? StartTime,
    string? EndTime,
    string? DisciplinePrimaryLang,
    string? DisciplineSecondaryLang,
    string? EventPrimaryLang,
    string? EventSecondaryLang,
    string? PhasePrimaryLang,
    string? PhaseSecondaryLang,
    string? UnitPrimaryLang,
    string? UnitSecondaryLang);

public record FooterLogo(string ImageData, string? AltText);

public record FooterData(
    IReadOnlyList<string> LeftTexts,
    IReadOnlyList<string> CenterTexts,
    IReadOnlyList<string> RightTexts,
    IReadOnlyList<FooterLogo> Logos);

public record TitleData(string ReportName, string GeneratedAt);

public record ReportData(
    HeaderData Header,
    FooterData Footer,
    TitleData Title,
    CommunicationContent Communication,
    object Body);

public interface IReportDataProvider
{
    string OrisCode { get; }
    string? Discipline { get; }

    Task<ReportData> GetDataAsync(Rsc rsc, ReportDataOptions options, CancellationToken ct = default);
}
