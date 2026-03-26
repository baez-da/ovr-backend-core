using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ErrorOr;
using FluentAssertions;
using NSubstitute;
using OVR.Modules.Reporting.Persistence;
using OVR.Modules.Reporting.Persistence.Documents;
using OVR.Modules.Reporting.Services;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.Reporting.Tests.Services;

public class ReportGeneratorTests
{
    // A valid 34-char RSC: ATH (discipline), W (gender), padded
    private const string ValidRsc = "ATHW------------------0001000100--";

    private readonly ITemplateResolver _templateResolver = Substitute.For<ITemplateResolver>();
    private readonly IScribanEngine _scribanEngine = Substitute.For<IScribanEngine>();
    private readonly IPdfRenderer _pdfRenderer = Substitute.For<IPdfRenderer>();
    private readonly IReportDataProvider _dataProvider = Substitute.For<IReportDataProvider>();
    private readonly IReportRecordRepository _recordRepository = Substitute.For<IReportRecordRepository>();
    private readonly DataProviderFactory _factory;
    private readonly ReportGenerator _sut;

    private static readonly ResolvedTemplates FakeTemplates = new(
        BodyTemplate: "<body/>",
        HeaderTemplate: "<header/>",
        FooterTemplate: "<footer/>",
        GlobalStyles: "",
        Partials: new Dictionary<string, string>());

    private static readonly ReportData FakeData = new(
        Header: new HeaderData("Event", "Final", "2024-08-01", null, null),
        Footer: new FooterData("2024-08-01T00:00:00Z", null, []),
        Communication: new CommunicationContent("Results", null, null),
        Body: new { });

    private static readonly RenderedHtml FakeHtml = new("<body/>", "<header/>", "<footer/>");

    public ReportGeneratorTests()
    {
        _dataProvider.OrisCode.Returns("ATH-RESULTS");
        _dataProvider.Discipline.Returns((string?)null);

        _factory = new DataProviderFactory([_dataProvider]);

        _sut = new ReportGenerator(
            _templateResolver,
            _scribanEngine,
            _pdfRenderer,
            _factory,
            _recordRepository);

        // Default happy path
        _templateResolver.ResolveAsync(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(FakeTemplates);

        _dataProvider.GetDataAsync(Arg.Any<Rsc>(), Arg.Any<ReportDataOptions>(), Arg.Any<CancellationToken>())
            .Returns(FakeData);

        _scribanEngine.RenderAsync(Arg.Any<ResolvedTemplates>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(FakeHtml);

        _pdfRenderer.RenderAsync(Arg.Any<RenderedHtml>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 0x25, 0x50, 0x44, 0x46 }); // %PDF

        _recordRepository.GetLatestAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ReportRecordDocument?)null);
    }

    [Fact]
    public async Task GenerateAsync_HappyPath_ReturnsPdfWithMetadata()
    {
        var options = new ReportDataOptions("eng");

        var result = await _sut.GenerateAsync(ValidRsc, "ATH-RESULTS", options, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Pdf.Should().NotBeEmpty();
        result.Value.Metadata.Rsc.Should().Be(ValidRsc);
        result.Value.Metadata.OrisCode.Should().Be("ATH-RESULTS");
        result.Value.Metadata.Discipline.Should().Be("ATH");
    }

    [Fact]
    public async Task GenerateAsync_NoExistingRecord_VersionIsOne()
    {
        _recordRepository.GetLatestAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ReportRecordDocument?)null);

        var result = await _sut.GenerateAsync(ValidRsc, "ATH-RESULTS", new ReportDataOptions("eng"), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Metadata.Version.Should().Be(1);
    }

    [Fact]
    public async Task GenerateAsync_SameHashAsExisting_KeepsExistingVersion()
    {
        var data = FakeData;
        var hash = ComputeHash(data);

        var existing = new ReportRecordDocument
        {
            Rsc = ValidRsc,
            OrisCode = "ATH-RESULTS",
            Version = 3,
            DataHash = hash
        };
        _recordRepository.GetLatestAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _sut.GenerateAsync(ValidRsc, "ATH-RESULTS", new ReportDataOptions("eng"), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Metadata.Version.Should().Be(3);
    }

    [Fact]
    public async Task GenerateAsync_DifferentHashFromExisting_IncrementsVersion()
    {
        var existing = new ReportRecordDocument
        {
            Rsc = ValidRsc,
            OrisCode = "ATH-RESULTS",
            Version = 2,
            DataHash = "old_hash_that_differs"
        };
        _recordRepository.GetLatestAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _sut.GenerateAsync(ValidRsc, "ATH-RESULTS", new ReportDataOptions("eng"), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Metadata.Version.Should().Be(3);
    }

    [Fact]
    public async Task GenerateAsync_TemplateNotFound_ReturnsError()
    {
        _templateResolver.ResolveAsync(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Error.NotFound("Reporting.TemplateNotFound", "Not found."));

        var result = await _sut.GenerateAsync(ValidRsc, "ATH-RESULTS", new ReportDataOptions("eng"), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Reporting.TemplateNotFound");
    }

    [Fact]
    public async Task GenerateAsync_PdfFailed_ReturnsError()
    {
        _pdfRenderer.RenderAsync(Arg.Any<RenderedHtml>(), Arg.Any<CancellationToken>())
            .Returns(Error.Failure("Reporting.PdfGenerationFailed", "Browser crashed."));

        var result = await _sut.GenerateAsync(ValidRsc, "ATH-RESULTS", new ReportDataOptions("eng"), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Reporting.PdfGenerationFailed");
    }

    [Fact]
    public async Task GenerateAsync_DataProviderNotFound_ReturnsError()
    {
        var factoryWithNoProviders = new DataProviderFactory(Array.Empty<IReportDataProvider>());
        var sut = new ReportGenerator(
            _templateResolver, _scribanEngine, _pdfRenderer, factoryWithNoProviders, _recordRepository);

        var result = await sut.GenerateAsync(ValidRsc, "ATH-RESULTS", new ReportDataOptions("eng"), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Reporting.DataProviderNotFound");
    }

    [Fact]
    public async Task GenerateAsync_PersistsRecordAfterGeneration()
    {
        var result = await _sut.GenerateAsync(ValidRsc, "ATH-RESULTS", new ReportDataOptions("eng"), CancellationToken.None);

        result.IsError.Should().BeFalse();
        await _recordRepository.Received(1).AddAsync(
            Arg.Is<ReportRecordDocument>(r =>
                r.Rsc == ValidRsc &&
                r.OrisCode == "ATH-RESULTS"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_ExtractsDisciplineFromRsc()
    {
        var result = await _sut.GenerateAsync(ValidRsc, "ATH-RESULTS", new ReportDataOptions("eng"), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Metadata.Discipline.Should().Be("ATH");
    }

    private static string ComputeHash(object data)
    {
        var json = JsonSerializer.Serialize(data);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
