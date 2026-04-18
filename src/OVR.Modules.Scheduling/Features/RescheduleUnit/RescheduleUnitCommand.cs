using ErrorOr;
using MediatR;

namespace OVR.Modules.Scheduling.Features.RescheduleUnit;

public sealed record RescheduleUnitCommand(
    string UnitRsc,
    string SessionCode,
    string LocationCode,
    DateTime StartTime,
    int OrderInSession,
    int OrderInLocation,
    string? Reason) : IRequest<ErrorOr<RescheduleUnitResponse>>;

public sealed record RescheduleUnitResponse(
    string UnitRsc,
    string EventRsc,
    string SessionCode,
    string LocationCode,
    DateTime StartTime,
    int OrderInSession,
    int OrderInLocation,
    string Status,
    DateTime ScheduledAt,
    DateTime? UpdatedAt);
