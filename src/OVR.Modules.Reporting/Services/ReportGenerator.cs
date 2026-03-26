using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ErrorOr;
using OVR.Modules.Reporting.Persistence;
using OVR.Modules.Reporting.Persistence.Documents;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.Reporting.Services;

internal sealed class ReportGenerator : IReportGenerator
{
    private readonly ITemplateResolver _templateResolver;
    private readonly IScribanEngine _scribanEngine;
    private readonly IPdfRenderer _pdfRenderer;
    private readonly DataProviderFactory _dataProviderFactory;
    private readonly IReportRecordRepository _recordRepository;

    public ReportGenerator(
        ITemplateResolver templateResolver,
        IScribanEngine scribanEngine,
        IPdfRenderer pdfRenderer,
        DataProviderFactory dataProviderFactory,
        IReportRecordRepository recordRepository)
    {
        _templateResolver = templateResolver;
        _scribanEngine = scribanEngine;
        _pdfRenderer = pdfRenderer;
        _dataProviderFactory = dataProviderFactory;
        _recordRepository = recordRepository;
    }

    public async Task<ErrorOr<GeneratedReport>> GenerateAsync(
        string rsc, string orisCode, ReportDataOptions options, CancellationToken ct = default)
    {
        // Extract discipline from RSC
        var rscValue = Rsc.Create(rsc);
        var discipline = rscValue.Discipline;

        // Resolve data provider
        var providerResult = _dataProviderFactory.Resolve(discipline, orisCode);
        if (providerResult.IsError)
            return providerResult.Errors;

        var provider = providerResult.Value.Provider;

        // Run template resolution and data fetch in parallel
        var templateTask = _templateResolver.ResolveAsync(discipline, orisCode, ct);
        var dataTask = provider.GetDataAsync(rscValue, options, ct);

        await Task.WhenAll(templateTask, dataTask);

        var templateResult = await templateTask;
        if (templateResult.IsError)
            return templateResult.Errors;

        var data = await dataTask;

        // Compute data hash for versioning
        var dataHash = ComputeHash(data);

        // Determine version
        var existingRecord = await _recordRepository.GetLatestAsync(rsc, orisCode, ct);
        var version = DetermineVersion(existingRecord, dataHash);

        // Render HTML with Scriban
        var rendered = await _scribanEngine.RenderAsync(templateResult.Value, data, ct);

        // Generate PDF
        var pdfResult = await _pdfRenderer.RenderAsync(rendered, ct);
        if (pdfResult.IsError)
            return pdfResult.Errors;

        var now = DateTime.UtcNow;
        var metadata = new ReportMetadata(
            Rsc: rsc,
            OrisCode: orisCode,
            Discipline: discipline,
            Version: version,
            DataHash: dataHash,
            GeneratedAt: now);

        // Persist record
        var record = new ReportRecordDocument
        {
            Rsc = rsc,
            OrisCode = orisCode,
            Discipline = discipline,
            Version = version,
            DataHash = dataHash,
            S3Key = string.Empty, // S3 upload handled separately
            GeneratedAt = now
        };
        await _recordRepository.AddAsync(record, ct);

        return new GeneratedReport(Pdf: pdfResult.Value, Metadata: metadata);
    }

    private static string ComputeHash(object data)
    {
        var json = JsonSerializer.Serialize(data);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static int DetermineVersion(ReportRecordDocument? existing, string dataHash)
    {
        if (existing is null)
            return 1;

        if (existing.DataHash == dataHash)
            return existing.Version;

        return existing.Version + 1;
    }
}
