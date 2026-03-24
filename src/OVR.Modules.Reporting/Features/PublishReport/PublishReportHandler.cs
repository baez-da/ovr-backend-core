using ErrorOr;
using MediatR;
using OVR.Modules.Reporting.Persistence;
using OVR.Modules.Reporting.Persistence.Documents;
using OVR.Modules.Reporting.Services;

namespace OVR.Modules.Reporting.Features.PublishReport;

public sealed class PublishReportHandler(
    IReportGenerator generator,
    IReportRecordRepository recordRepository,
    IS3Storage s3Storage)
    : IRequestHandler<PublishReportCommand, ErrorOr<PublishReportResponse>>
{
    public async Task<ErrorOr<PublishReportResponse>> Handle(
        PublishReportCommand request, CancellationToken cancellationToken)
    {
        var options = new ReportDataOptions(Language: "eng");
        var generateResult = await generator.GenerateAsync(request.Rsc, request.OrisCode, options, cancellationToken);
        if (generateResult.IsError)
            return generateResult.Errors;

        var report = generateResult.Value;

        // Check if an existing record with the same hash exists
        var existing = await recordRepository.GetLatestAsync(request.Rsc, request.OrisCode, cancellationToken);

        if (!request.ForceGenerate && existing is not null && existing.DataHash == report.Metadata.DataHash)
        {
            // Same content — return existing URL without re-uploading
            var existingUrl = s3Storage.GetUrl(existing.S3Key);
            return new PublishReportResponse(existingUrl, existing.Version, IsNew: false);
        }

        // Upload to S3
        var s3Key = $"reports/{request.Rsc}/{request.OrisCode}/v{report.Metadata.Version}.pdf";
        var uploadResult = await s3Storage.UploadAsync(s3Key, report.Pdf, "application/pdf", cancellationToken);

        // Persist record
        var record = new ReportRecordDocument
        {
            Rsc = request.Rsc,
            OrisCode = request.OrisCode,
            DisciplineCode = report.Metadata.DisciplineCode,
            Version = report.Metadata.Version,
            DataHash = report.Metadata.DataHash,
            S3Key = s3Key,
            GeneratedAt = report.Metadata.GeneratedAt
        };
        await recordRepository.AddAsync(record, cancellationToken);

        return new PublishReportResponse(uploadResult, report.Metadata.Version, IsNew: true);
    }
}
