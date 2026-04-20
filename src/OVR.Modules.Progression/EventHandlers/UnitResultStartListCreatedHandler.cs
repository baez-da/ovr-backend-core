using MediatR;
using Microsoft.Extensions.Logging;
using OVR.Modules.Progression.Persistence;
using OVR.SharedKernel.Domain.Events.Integration;

namespace OVR.Modules.Progression.EventHandlers;

public sealed class UnitResultStartListCreatedHandler(
    IBracketProgressionRepository repository,
    IPublisher publisher,
    ILogger<UnitResultStartListCreatedHandler> logger)
    : INotificationHandler<UnitResultStartListCreatedEvent>
{
    public async Task Handle(UnitResultStartListCreatedEvent notification, CancellationToken ct)
    {
        var agg = await repository.GetByEventAsync(notification.EventRsc, ct);
        if (agg is null)
        {
            logger.LogInformation(
                "BracketProgression not found for event {EventRsc} (unit {UnitRsc}) on StartListCreated.",
                notification.EventRsc, notification.UnitRsc);
            return;
        }

        var flushed = agg.MarkTargetReady(notification.UnitRsc);
        await repository.ReplaceAsync(agg, ct);

        foreach (var p in flushed)
        {
            await publisher.Publish(new CompetitorAdvancedEvent(
                EventRsc: notification.EventRsc,
                TargetUnitRsc: p.TargetUnitRsc,
                TargetSlot: p.TargetSlot,
                ParticipantId: p.ParticipantId,
                SourceUnitRsc: p.SourceUnitRsc,
                AdvancedAt: DateTime.UtcNow), ct);
        }
    }
}
