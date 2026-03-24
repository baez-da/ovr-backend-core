using ErrorOr;
using MediatR;
using OVR.Modules.Reporting.Services;

namespace OVR.Modules.Reporting.Features.PreviewDailySchedule;

public record PreviewDailyScheduleQuery(string Rsc, string OrisCode, DateOnly Date)
    : IRequest<ErrorOr<GeneratedReport>>;
