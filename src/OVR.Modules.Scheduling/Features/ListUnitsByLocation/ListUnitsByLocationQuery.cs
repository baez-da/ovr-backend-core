using ErrorOr;
using MediatR;

namespace OVR.Modules.Scheduling.Features.ListUnitsByLocation;

public sealed record ListUnitsByLocationQuery(
    string LocationCode,
    DateOnly? Date) : IRequest<ErrorOr<IReadOnlyList<ScheduledUnitDto>>>;

public sealed record ScheduledUnitDto(
    string UnitRsc,
    string EventRsc,
    string SessionCode,
    string LocationCode,
    DateTime StartTime,
    int OrderInSession,
    int OrderInLocation,
    string Status,
    DateTime ScheduledAt);
