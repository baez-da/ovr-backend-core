using ErrorOr;
using MediatR;
using OVR.Modules.Reporting.Errors;
using OVR.Modules.Reporting.Persistence;
using OVR.Modules.Reporting.Services;

namespace OVR.Modules.Reporting.Features.GetReport;

public sealed class GetReportHandler(
    IReportRecordRepository recordRepository,
    IS3Storage s3Storage)
    : IRequestHandler<GetReportQuery, ErrorOr<GetReportResponse>>
{
    public async Task<ErrorOr<GetReportResponse>> Handle(GetReportQuery request, CancellationToken cancellationToken)
    {
        var record = await recordRepository.GetLatestAsync(request.Rsc, request.OrisCode, cancellationToken);
        if (record is null)
            return ReportingErrors.ReportNotFound(request.Rsc, request.OrisCode);

        var url = s3Storage.GetUrl(record.S3Key);
        return new GetReportResponse(url, record.Version, record.GeneratedAt);
    }
}
