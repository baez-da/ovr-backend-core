using FluentAssertions;
using NSubstitute;
using OVR.Modules.Reporting.Persistence;
using OVR.Modules.Reporting.Persistence.Documents;
using OVR.Modules.Reporting.Services;

namespace OVR.Modules.Reporting.Tests.Services;

public class TemplateResolverTests
{
    private readonly IReportTemplateRepository _templateRepo = Substitute.For<IReportTemplateRepository>();
    private readonly IReportLayoutRepository _layoutRepo = Substitute.For<IReportLayoutRepository>();
    private readonly IReportPartialRepository _partialRepo = Substitute.For<IReportPartialRepository>();
    private readonly TemplateResolver _sut;

    private static readonly ReportLayoutDocument HeaderDoc = new() { Component = "header", Content = "<header/>" };
    private static readonly ReportLayoutDocument FooterDoc = new() { Component = "footer", Content = "<footer/>" };
    private static readonly ReportLayoutDocument StyleDoc = new() { Component = "style", Content = "body{}" };

    public TemplateResolverTests()
    {
        _sut = new TemplateResolver(_templateRepo, _layoutRepo, _partialRepo);

        // Default: no partials
        _partialRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ReportPartialDocument>());

        // Default layouts (null discipline)
        _layoutRepo.FindAsync("header", null, Arg.Any<CancellationToken>()).Returns(HeaderDoc);
        _layoutRepo.FindAsync("footer", null, Arg.Any<CancellationToken>()).Returns(FooterDoc);
        _layoutRepo.FindAsync("style", null, Arg.Any<CancellationToken>()).Returns(StyleDoc);

        // Default: discipline-specific layouts not found
        _layoutRepo.FindAsync(Arg.Any<string>(), Arg.Is<string?>(d => d != null), Arg.Any<CancellationToken>())
            .Returns((ReportLayoutDocument?)null);
    }

    [Fact]
    public async Task ResolveAsync_DisciplineSpecificTemplate_ReturnsDisciplineTemplate()
    {
        var disciplineTemplate = new ReportTemplateDocument
        {
            OrisCode = "ATH-RESULTS", DisciplineCode = "ATH", Content = "<discipline-body/>"
        };

        _templateRepo.FindAsync("ATH-RESULTS", "ATH", Arg.Any<CancellationToken>())
            .Returns(disciplineTemplate);

        var result = await _sut.ResolveAsync("ATH", "ATH-RESULTS", CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.BodyTemplate.Should().Be("<discipline-body/>");
    }

    [Fact]
    public async Task ResolveAsync_NoDisciplineSpecificTemplate_FallsBackToGeneric()
    {
        var genericTemplate = new ReportTemplateDocument
        {
            OrisCode = "ATH-RESULTS", DisciplineCode = null, Content = "<generic-body/>"
        };

        // Discipline-specific returns null
        _templateRepo.FindAsync("ATH-RESULTS", "ATH", Arg.Any<CancellationToken>())
            .Returns((ReportTemplateDocument?)null);
        // Generic returns template
        _templateRepo.FindAsync("ATH-RESULTS", null, Arg.Any<CancellationToken>())
            .Returns(genericTemplate);

        var result = await _sut.ResolveAsync("ATH", "ATH-RESULTS", CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.BodyTemplate.Should().Be("<generic-body/>");
    }

    [Fact]
    public async Task ResolveAsync_NoTemplateFound_ReturnsTemplateNotFoundError()
    {
        _templateRepo.FindAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((ReportTemplateDocument?)null);

        var result = await _sut.ResolveAsync("ATH", "ATH-RESULTS", CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Reporting.TemplateNotFound");
    }

    [Fact]
    public async Task ResolveAsync_HeaderNotFound_ReturnsLayoutNotFoundError()
    {
        var template = new ReportTemplateDocument { OrisCode = "ATH-RESULTS", Content = "<body/>" };
        _templateRepo.FindAsync("ATH-RESULTS", null, Arg.Any<CancellationToken>()).Returns(template);

        _layoutRepo.FindAsync("header", null, Arg.Any<CancellationToken>())
            .Returns((ReportLayoutDocument?)null);

        var result = await _sut.ResolveAsync(null, "ATH-RESULTS", CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Reporting.LayoutNotFound");
    }

    [Fact]
    public async Task ResolveAsync_FooterNotFound_ReturnsLayoutNotFoundError()
    {
        var template = new ReportTemplateDocument { OrisCode = "ATH-RESULTS", Content = "<body/>" };
        _templateRepo.FindAsync("ATH-RESULTS", null, Arg.Any<CancellationToken>()).Returns(template);

        _layoutRepo.FindAsync("footer", null, Arg.Any<CancellationToken>())
            .Returns((ReportLayoutDocument?)null);

        var result = await _sut.ResolveAsync(null, "ATH-RESULTS", CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Reporting.LayoutNotFound");
    }

    [Fact]
    public async Task ResolveAsync_StyleNotFound_ReturnsEmptyStyles()
    {
        var template = new ReportTemplateDocument { OrisCode = "ATH-RESULTS", Content = "<body/>" };
        _templateRepo.FindAsync("ATH-RESULTS", null, Arg.Any<CancellationToken>()).Returns(template);

        _layoutRepo.FindAsync("style", null, Arg.Any<CancellationToken>())
            .Returns((ReportLayoutDocument?)null);

        var result = await _sut.ResolveAsync(null, "ATH-RESULTS", CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.GlobalStyles.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_WithPartials_IncludesPartialsInResult()
    {
        var template = new ReportTemplateDocument { OrisCode = "ATH-RESULTS", Content = "<body/>" };
        _templateRepo.FindAsync("ATH-RESULTS", null, Arg.Any<CancellationToken>()).Returns(template);

        var partials = new[]
        {
            new ReportPartialDocument { Name = "row", Content = "<tr>{{item}}</tr>" },
            new ReportPartialDocument { Name = "logo", Content = "<img src='logo.png'/>" }
        };
        _partialRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(partials);

        var result = await _sut.ResolveAsync(null, "ATH-RESULTS", CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Partials.Should().HaveCount(2);
        result.Value.Partials["row"].Should().Be("<tr>{{item}}</tr>");
        result.Value.Partials["logo"].Should().Be("<img src='logo.png'/>");
    }

    [Fact]
    public async Task ResolveAsync_IncludesHeaderAndFooter()
    {
        var template = new ReportTemplateDocument { OrisCode = "ATH-RESULTS", Content = "<body/>" };
        _templateRepo.FindAsync("ATH-RESULTS", null, Arg.Any<CancellationToken>()).Returns(template);

        var result = await _sut.ResolveAsync(null, "ATH-RESULTS", CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.HeaderTemplate.Should().Be("<header/>");
        result.Value.FooterTemplate.Should().Be("<footer/>");
        result.Value.GlobalStyles.Should().Be("body{}");
    }
}
