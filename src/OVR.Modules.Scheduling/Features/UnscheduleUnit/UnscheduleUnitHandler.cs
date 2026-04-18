using ErrorOr;
using MediatR;
using OVR.Modules.Scheduling.Errors;
using OVR.Modules.Scheduling.Persistence;
using OVR.SharedKernel.Domain.Events.Integration;

namespace OVR.Modules.Scheduling.Features.UnscheduleUnit;

public sealed class UnscheduleUnitHandler(
    IUnitScheduleRepository repository,
    IPublisher publisher)
    : IRequestHandler<UnscheduleUnitCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(
        UnscheduleUnitCommand request,
        CancellationToken ct)
    {
        var schedule = await repository.GetByUnitRscAsync(request.UnitRsc, ct);
        if (schedule is null)
            return SchedulingErrors.UnitScheduleNotFound(request.UnitRsc);

        var eventRsc = schedule.EventRsc.Value;
        await repository.DeleteAsync(request.UnitRsc, ct);

        await publisher.Publish(
            new UnitUnscheduledEvent(request.UnitRsc, eventRsc, DateTime.UtcNow), ct);

        return Result.Success;
    }
}
