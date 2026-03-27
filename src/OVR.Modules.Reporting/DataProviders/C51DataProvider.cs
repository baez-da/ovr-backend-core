using OVR.Modules.Reporting.Services;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.Reporting.DataProviders;

/// <summary>
/// Generic demo data provider for C51 (Start List) reports.
/// Returns hardcoded demo data suitable for template development and integration testing.
/// </summary>
public sealed class C51DataProvider : IReportDataProvider
{
    public string OrisCode => "C51";
    public string? Discipline => null; // generic — applies to any discipline

    public Task<ReportData> GetDataAsync(Rsc rsc, ReportDataOptions options, CancellationToken ct = default)
    {
        var discipline = rsc.Discipline;
        var gender = rsc.Gender switch
        {
            'W' => "Women",
            'M' => "Men",
            'X' => "Mixed",
            _ => "Open"
        };

        var eventTrimmed = rsc.Event.TrimEnd('-');
        var phaseTrimmed = rsc.Phase.TrimEnd('-');

        var eventLabel = string.IsNullOrEmpty(eventTrimmed) ? "Demo Event" : eventTrimmed;
        var phaseLabel = string.IsNullOrEmpty(phaseTrimmed) ? "Heat 1" : phaseTrimmed;
        var dateLabel = (options.Date?.ToString("dddd, MMMM d, yyyy") ?? DateTime.UtcNow.ToString("dddd, MMMM d, yyyy")).ToUpperInvariant();

        var header = new HeaderData(
            LogoImageData: null,
            PictogramImageData: null,
            VenueEn: "Demo Venue",
            VenueLocal: null,
            DateStr: dateLabel,
            StartTime: "10:00",
            EndTime: null,
            DisciplinePrimaryLang: discipline,
            DisciplineSecondaryLang: null,
            EventPrimaryLang: $"{gender} {eventLabel}",
            EventSecondaryLang: null,
            PhasePrimaryLang: phaseLabel,
            PhaseSecondaryLang: null,
            UnitPrimaryLang: null,
            UnitSecondaryLang: null);

        var footer = new FooterData(
            LeftTexts: [$"Official Start List — {OrisCode}"],
            CenterTexts: [$"RSC: {rsc.Value}"],
            RightTexts: [$"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm}"],
            Logos: []);

        var body = new
        {
            Rsc = rsc.Value,
            OrisCode,
            Discipline = discipline,
            Gender = gender,
            Entries = new[]
            {
                new { Bib = "101", Name = "SMITH John", Organisation = "USA", StartTime = "10:00" },
                new { Bib = "102", Name = "GARCIA Maria", Organisation = "ESP", StartTime = "10:02" },
                new { Bib = "103", Name = "MÜLLER Hans", Organisation = "GER", StartTime = "10:04" },
                new { Bib = "104", Name = "YAMAMOTO Keiko", Organisation = "JPN", StartTime = "10:06" },
                new { Bib = "105", Name = "SILVA Carlos", Organisation = "BRA", StartTime = "10:08" },
                new { Bib = "106", Name = "JOHNSON Emily", Organisation = "GBR", StartTime = "10:10" },
                new { Bib = "107", Name = "DUBOIS Pierre", Organisation = "FRA", StartTime = "10:12" },
                new { Bib = "108", Name = "CHEN Wei", Organisation = "CHN", StartTime = "10:14" }
            }
        };

        var communication = new CommunicationContent(
            Title: $"Start List — {discipline} {gender}",
            Subtitle: $"RSC: {rsc.Value}",
            Description: "Official start list for the competition unit.");

        var title = new TitleData(
            ReportName: "Start List",
            GeneratedAt: DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm"));

        var reportData = new ReportData(
            Header: header,
            Footer: footer,
            Title: title,
            Communication: communication,
            Body: body);

        return Task.FromResult(reportData);
    }
}
