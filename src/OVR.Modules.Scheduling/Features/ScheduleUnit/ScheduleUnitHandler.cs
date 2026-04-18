using ErrorOr;
using MediatR;
using OVR.Modules.Scheduling.Domain;
using OVR.Modules.Scheduling.Errors;
using OVR.Modules.Scheduling.Persistence;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.Scheduling.Features.ScheduleUnit;

public sealed class ScheduleUnitHandler(
    ISessionRepository sessionRepository,
    IUnitScheduleRepository scheduleRepository,
    IPublisher publisher,
    IScheduleCollisionDetector collisionDetector)
    : IRequestHandler<ScheduleUnitCommand, ErrorOr<ScheduleUnitResponse>>
{
    public async Task<ErrorOr<ScheduleUnitResponse>> Handle(
        ScheduleUnitCommand request,
        CancellationToken ct)
    {
        var session = await sessionRepository.GetByCodeAsync(request.SessionCode, ct);
        if (session is null)
            return SchedulingErrors.SessionNotFound(request.SessionCode);

        if (request.StartTime < session.StartDate || request.StartTime > session.EndDate)
            return SchedulingErrors.StartTimeOutsideSession(
                request.StartTime, session.StartDate, session.EndDate);

        var existing = await scheduleRepository.GetByUnitRscAsync(request.UnitRsc, ct);
        if (existing is not null)
            return SchedulingErrors.UnitAlreadyScheduled(request.UnitRsc);

        var collisionResult = await collisionDetector.EnsureNoCollisionAsync(
            request.LocationCode, request.StartTime, excludeUnitRsc: null, ct);
        if (collisionResult.IsError)
            return collisionResult.Errors;

        var unitRsc = Rsc.Create(request.UnitRsc);
        var schedule = UnitSchedule.Create(
            unitRsc, request.SessionCode, request.LocationCode,
            request.StartTime, request.OrderInSession, request.OrderInLocation);

        await scheduleRepository.AddAsync(schedule, ct);

        foreach (var e in schedule.DomainEvents)
            await publisher.Publish(e, ct);
        schedule.ClearDomainEvents();

        return new ScheduleUnitResponse(
            UnitRsc: schedule.UnitRsc.Value,
            EventRsc: schedule.EventRsc.Value,
            SessionCode: schedule.SessionCode,
            LocationCode: schedule.LocationCode,
            StartTime: schedule.StartTime,
            OrderInSession: schedule.OrderInSession,
            OrderInLocation: schedule.OrderInLocation,
            Status: schedule.Status.ToString(),
            ScheduledAt: schedule.ScheduledAt);
    }
}
