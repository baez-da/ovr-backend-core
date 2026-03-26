using ErrorOr;
using MediatR;
using OVR.Modules.Reporting.Features.PublishReport;
using OVR.Modules.Reporting.Persistence;
using OVR.Modules.Reporting.Persistence.Documents;
using OVR.Modules.Reporting.Services;

namespace OVR.Modules.Reporting.Features.PublishCommunication;

public sealed class PublishCommunicationHandler(
    IReportGenerator generator,
    IReportRecordRepository recordRepository,
    IS3Storage s3Storage)
    : IRequestHandler<PublishCommunicationCommand, ErrorOr<PublishReportResponse>>
{
    public async Task<ErrorOr<PublishReportResponse>> Handle(
        PublishCommunicationCommand request, CancellationToken cancellationToken)
    {
        var options = new ReportDataOptions(Language: "eng", Content: request.Content);
        var generateResult = await generator.GenerateAsync(request.Rsc, request.OrisCode, options, cancellationToken);
        if (generateResult.IsError)
            return generateResult.Errors;

        var report = generateResult.Value;

        var existing = await recordRepository.GetLatestAsync(request.Rsc, request.OrisCode, cancellationToken);

        if (!request.ForceGenerate && existing is not null && existing.DataHash == report.Metadata.DataHash)
        {
            var existingUrl = s3Storage.GetUrl(existing.S3Key);
            return new PublishReportResponse(existingUrl, existing.Version, IsNew: false);
        }

        var s3Key = $"reports/{request.Rsc}/{request.OrisCode}/v{report.Metadata.Version}.pdf";
        var uploadResult = await s3Storage.UploadAsync(s3Key, report.Pdf, "application/pdf", cancellationToken);

        var record = new ReportRecordDocument
        {
            Rsc = request.Rsc,
            OrisCode = request.OrisCode,
            Discipline = report.Metadata.Discipline,
            Version = report.Metadata.Version,
            DataHash = report.Metadata.DataHash,
            S3Key = s3Key,
            GeneratedAt = report.Metadata.GeneratedAt
        };
        await recordRepository.AddAsync(record, cancellationToken);

        return new PublishReportResponse(uploadResult, report.Metadata.Version, IsNew: true);
    }
}
