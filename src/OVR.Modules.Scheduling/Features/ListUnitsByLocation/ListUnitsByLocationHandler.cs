using ErrorOr;
using MediatR;
using OVR.Modules.Scheduling.Persistence;

namespace OVR.Modules.Scheduling.Features.ListUnitsByLocation;

public sealed class ListUnitsByLocationHandler(IUnitScheduleRepository repository)
    : IRequestHandler<ListUnitsByLocationQuery, ErrorOr<IReadOnlyList<ScheduledUnitDto>>>
{
    public async Task<ErrorOr<IReadOnlyList<ScheduledUnitDto>>> Handle(
        ListUnitsByLocationQuery request,
        CancellationToken ct)
    {
        var date = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var schedules = await repository.ListByLocationAndDateAsync(
            request.LocationCode, date, ct);

        var dtos = schedules
            .Select(s => new ScheduledUnitDto(
                UnitRsc: s.UnitRsc.Value,
                EventRsc: s.EventRsc.Value,
                SessionCode: s.SessionCode,
                LocationCode: s.LocationCode,
                StartTime: s.StartTime,
                OrderInSession: s.OrderInSession,
                OrderInLocation: s.OrderInLocation,
                Status: s.Status.ToString(),
                ScheduledAt: s.ScheduledAt))
            .ToList();

        return dtos.AsReadOnly();
    }
}
