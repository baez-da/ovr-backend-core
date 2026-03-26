using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OVR.Modules.Reporting.DataProviders;
using OVR.Modules.Reporting.Persistence;
using OVR.Modules.Reporting.Persistence.Documents;
using OVR.Modules.Reporting.Services;

namespace OVR.Modules.Reporting.Tests.Integration;

/// <summary>
/// End-to-end PDF generation test using real services (Scriban + Playwright).
/// Requires Playwright chromium to be installed on the test machine.
/// Filter with: dotnet test --filter "Category=Integration"
/// </summary>
[Trait("Category", "Integration")]
public sealed class PdfGenerationTests
{
    // A valid 34-char RSC: ATH discipline, Women gender, unit-level
    private const string ValidRsc = "ATHW------------------0001000100--";

    private const string BodyTemplate = """
        <div class="report-section">
          <h2>{{ header.event_name }}</h2>
          <p>{{ header.phase_name }} — {{ header.date }}</p>
          <table class="start-list-table">
            <thead>
              <tr><th>Bib</th><th>Name</th><th>Organisation</th><th>Start Time</th></tr>
            </thead>
            <tbody>
              {{ for entry in body.entries }}
              <tr>
                <td>{{ entry.bib }}</td>
                <td>{{ entry.name }}</td>
                <td>{{ entry.organisation }}</td>
                <td>{{ entry.start_time }}</td>
              </tr>
              {{ end }}
            </tbody>
          </table>
        </div>
        """;

    private const string HeaderTemplate = """
        <div style="width:100%;font-family:Arial,sans-serif;font-size:9px;padding:4px 16px;display:flex;justify-content:space-between;border-bottom:1px solid #ccc;box-sizing:border-box;">
          <div style="font-weight:bold;">{{ header.venue_name }}</div>
          <div style="font-weight:bold;">{{ header.event_name }}</div>
          <div>{{ header.date }}</div>
        </div>
        """;

    private const string FooterTemplate = """
        <div style="width:100%;font-family:Arial,sans-serif;font-size:8px;padding:4px 16px;display:flex;justify-content:space-between;border-top:1px solid #ccc;box-sizing:border-box;color:#555;">
          <div>{{ footer.official_text }}</div>
          <div><span class="pageNumber"></span> / <span class="totalPages"></span></div>
          <div>Generated: {{ footer.generated_at }}</div>
        </div>
        """;

    private const string GlobalStyles = """
        body { font-family: Arial, Helvetica, sans-serif; font-size: 10pt; color: #1a1a1a; margin: 0; padding: 0 16px; }
        h2 { font-size: 14pt; font-weight: bold; color: #003366; }
        table { width: 100%; border-collapse: collapse; }
        th { background-color: #003366; color: #fff; padding: 5px 8px; text-align: left; }
        td { padding: 4px 8px; border-bottom: 1px solid #ddd; }
        """;

    [Fact]
    public async Task GenerateAsync_C51WithRealPlaywright_ReturnsPdfBytes()
    {
        // ── Arrange ─────────────────────────────────────────────────────────

        // Mock repositories
        var templateRepo = Substitute.For<IReportTemplateRepository>();
        templateRepo
            .FindAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ReportTemplateDocument
            {
                OrisCode = "C51",
                Discipline = null,
                Content = BodyTemplate,
                UpdatedAt = DateTime.UtcNow
            });

        var layoutRepo = Substitute.For<IReportLayoutRepository>();
        layoutRepo
            .FindAsync("header", Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ReportLayoutDocument { Component = "header", Content = HeaderTemplate, UpdatedAt = DateTime.UtcNow });
        layoutRepo
            .FindAsync("footer", Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ReportLayoutDocument { Component = "footer", Content = FooterTemplate, UpdatedAt = DateTime.UtcNow });
        layoutRepo
            .FindAsync("style", Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ReportLayoutDocument { Component = "style", Content = GlobalStyles, UpdatedAt = DateTime.UtcNow });

        var partialRepo = Substitute.For<IReportPartialRepository>();
        partialRepo
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ReportPartialDocument>>([]));

        var recordRepo = Substitute.For<IReportRecordRepository>();
        recordRepo
            .GetLatestAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ReportRecordDocument?)null);
        recordRepo
            .AddAsync(Arg.Any<ReportRecordDocument>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Real services
        var templateResolver = new TemplateResolver(templateRepo, layoutRepo, partialRepo);
        var scribanEngine = new ScribanEngine();
        var dataProvider = new C51DataProvider();
        var factory = new DataProviderFactory([dataProvider]);

        // Playwright PDF renderer — empty config means local Playwright install
        var config = new ConfigurationBuilder().Build();
        await using var pdfRenderer = new PlaywrightPdfRenderer(config);

        var generator = new ReportGenerator(
            templateResolver,
            scribanEngine,
            pdfRenderer,
            factory,
            recordRepo);

        var options = new ReportDataOptions(Language: "eng");

        // ── Act ──────────────────────────────────────────────────────────────
        var result = await generator.GenerateAsync(
            rsc: ValidRsc,
            orisCode: "C51",
            options: options,
            ct: CancellationToken.None);

        // ── Assert ───────────────────────────────────────────────────────────
        result.IsError.Should().BeFalse(
            because: result.IsError ? string.Join("; ", result.Errors.Select(e => e.Description)) : "no error");

        var pdf = result.Value.Pdf;
        pdf.Should().NotBeEmpty();

        // PDF magic bytes: %PDF
        pdf[0].Should().Be(0x25); // %
        pdf[1].Should().Be(0x50); // P
        pdf[2].Should().Be(0x44); // D
        pdf[3].Should().Be(0x46); // F
    }
}
