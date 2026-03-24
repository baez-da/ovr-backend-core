using ErrorOr;
using MediatR;
using OVR.Modules.Reporting.Services;

namespace OVR.Modules.Reporting.Features.PreviewDailySchedule;

public sealed class PreviewDailyScheduleHandler(IReportGenerator generator)
    : IRequestHandler<PreviewDailyScheduleQuery, ErrorOr<GeneratedReport>>
{
    public Task<ErrorOr<GeneratedReport>> Handle(PreviewDailyScheduleQuery request, CancellationToken cancellationToken)
    {
        var options = new ReportDataOptions(Language: "eng", Date: request.Date);
        return generator.GenerateAsync(request.Rsc, request.OrisCode, options, cancellationToken);
    }
}
