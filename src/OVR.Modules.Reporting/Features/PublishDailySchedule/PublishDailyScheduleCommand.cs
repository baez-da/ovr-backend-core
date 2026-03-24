using ErrorOr;
using MediatR;
using OVR.Modules.Reporting.Features.PublishReport;

namespace OVR.Modules.Reporting.Features.PublishDailySchedule;

public record PublishDailyScheduleCommand(
    string Rsc, string OrisCode, DateOnly Date, bool ForceGenerate = false)
    : IRequest<ErrorOr<PublishReportResponse>>;
