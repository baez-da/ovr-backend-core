using ErrorOr;
using MediatR;
using OVR.Modules.Reporting.Services;

namespace OVR.Modules.Reporting.Features.PreviewReport;

public sealed class PreviewReportHandler(IReportGenerator generator)
    : IRequestHandler<PreviewReportQuery, ErrorOr<GeneratedReport>>
{
    public Task<ErrorOr<GeneratedReport>> Handle(PreviewReportQuery request, CancellationToken cancellationToken)
    {
        var options = new ReportDataOptions(Language: "eng");
        return generator.GenerateAsync(request.Rsc, request.OrisCode, options, cancellationToken);
    }
}
