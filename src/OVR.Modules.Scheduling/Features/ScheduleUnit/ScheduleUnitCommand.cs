using ErrorOr;
using MediatR;

namespace OVR.Modules.Scheduling.Features.ScheduleUnit;

public sealed record ScheduleUnitCommand(
    string SessionCode,
    string UnitRsc,
    string LocationCode,
    DateTime StartTime,
    int OrderInSession,
    int OrderInLocation) : IRequest<ErrorOr<ScheduleUnitResponse>>;

public sealed record ScheduleUnitResponse(
    string UnitRsc,
    string EventRsc,
    string SessionCode,
    string LocationCode,
    DateTime StartTime,
    int OrderInSession,
    int OrderInLocation,
    string Status,
    DateTime ScheduledAt);
