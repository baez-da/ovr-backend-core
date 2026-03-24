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
    public string? DisciplineCode => null; // generic — applies to any discipline

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

        var header = new HeaderData(
            EventName: $"{discipline} {gender} — {(string.IsNullOrEmpty(eventTrimmed) ? "Demo Event" : eventTrimmed)}",
            PhaseName: string.IsNullOrEmpty(phaseTrimmed) ? "Heat 1" : phaseTrimmed,
            Date: options.Date?.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.ToString("yyyy-MM-dd"),
            VenueName: "Demo Venue",
            LogoUrl: null);

        var footer = new FooterData(
            GeneratedAt: DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            OfficialText: $"Official Start List — {rsc.Value} — {OrisCode}",
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

        var reportData = new ReportData(
            Header: header,
            Footer: footer,
            Communication: communication,
            Body: body);

        return Task.FromResult(reportData);
    }
}
