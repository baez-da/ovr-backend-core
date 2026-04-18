using ErrorOr;
using MediatR;
using OVR.Modules.Scheduling.Domain;
using OVR.Modules.Scheduling.Errors;
using OVR.Modules.Scheduling.Persistence;

namespace OVR.Modules.Scheduling.Features.RescheduleUnit;

public sealed class RescheduleUnitHandler(
    ISessionRepository sessionRepository,
    IUnitScheduleRepository scheduleRepository,
    IPublisher publisher,
    IScheduleCollisionDetector collisionDetector)
    : IRequestHandler<RescheduleUnitCommand, ErrorOr<RescheduleUnitResponse>>
{
    public async Task<ErrorOr<RescheduleUnitResponse>> Handle(
        RescheduleUnitCommand request,
        CancellationToken ct)
    {
        var schedule = await scheduleRepository.GetByUnitRscAsync(request.UnitRsc, ct);
        if (schedule is null)
            return SchedulingErrors.UnitScheduleNotFound(request.UnitRsc);

        var session = await sessionRepository.GetByCodeAsync(request.SessionCode, ct);
        if (session is null)
            return SchedulingErrors.SessionNotFound(request.SessionCode);

        if (request.StartTime < session.StartDate || request.StartTime > session.EndDate)
            return SchedulingErrors.StartTimeOutsideSession(
                request.StartTime, session.StartDate, session.EndDate);

        var collisionResult = await collisionDetector.EnsureNoCollisionAsync(
            request.LocationCode, request.StartTime,
            excludeUnitRsc: request.UnitRsc, ct);
        if (collisionResult.IsError)
            return collisionResult.Errors;

        schedule.Reschedule(
            request.SessionCode, request.LocationCode, request.StartTime,
            request.OrderInSession, request.OrderInLocation, request.Reason);

        await scheduleRepository.UpdateAsync(schedule, ct);

        foreach (var e in schedule.DomainEvents)
            await publisher.Publish(e, ct);
        schedule.ClearDomainEvents();

        return new RescheduleUnitResponse(
            UnitRsc: schedule.UnitRsc.Value,
            EventRsc: schedule.EventRsc.Value,
            SessionCode: schedule.SessionCode,
            LocationCode: schedule.LocationCode,
            StartTime: schedule.StartTime,
            OrderInSession: schedule.OrderInSession,
            OrderInLocation: schedule.OrderInLocation,
            Status: schedule.Status.ToString(),
            ScheduledAt: schedule.ScheduledAt,
            UpdatedAt: schedule.UpdatedAt);
    }
}
