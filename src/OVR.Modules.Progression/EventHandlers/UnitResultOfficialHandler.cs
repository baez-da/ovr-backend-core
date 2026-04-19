using MediatR;
using Microsoft.Extensions.Logging;
using OVR.Modules.Progression.Domain;
using OVR.Modules.Progression.Persistence;
using OVR.SharedKernel.Domain;
using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.Progression;

namespace OVR.Modules.Progression.EventHandlers;

public sealed class UnitResultOfficialHandler(
    IBracketProgressionRepository repository,
    IPublisher publisher,
    ILogger<UnitResultOfficialHandler> logger)
    : INotificationHandler<UnitResultOfficialEvent>
{
    public async Task Handle(UnitResultOfficialEvent notification, CancellationToken ct)
    {
        var eventRsc = RscParser.GetEventRscFromUnitRsc(notification.UnitRsc);
        var agg = await repository.GetByEventAsync(eventRsc, ct);
        if (agg is null)
        {
            logger.LogError(
                "BracketProgression not found for event {EventRsc} (unit {UnitRsc}).",
                eventRsc, notification.UnitRsc);
            return;
        }

        var outcome = agg.RecordAdvancement(
            sourceUnitRsc: notification.UnitRsc,
            outcome: Outcome.W,
            participantId: notification.WinnerParticipantId);

        await repository.ReplaceAsync(agg, ct);

        switch (outcome)
        {
            case AdvancementOutcome.Ready ready:
                await publisher.Publish(new CompetitorAdvancedEvent(
                    EventRsc: eventRsc,
                    TargetUnitRsc: ready.Edge.TargetUnitRsc,
                    TargetSlot: ready.Edge.TargetSlot,
                    ParticipantId: ready.ParticipantId,
                    SourceUnitRsc: notification.UnitRsc,
                    AdvancedAt: DateTime.UtcNow), ct);
                break;

            case AdvancementOutcome.Buffered:
                // Persisted. Nothing to publish until target is ready.
                break;

            case AdvancementOutcome.Terminal terminal when terminal.ChampionParticipantId is { } champion:
                await publisher.Publish(new EventProgressionCompletedEvent(
                    EventRsc: eventRsc,
                    FinalUnitRsc: notification.UnitRsc,
                    ChampionParticipantId: champion,
                    CompletedAt: DateTime.UtcNow), ct);
                break;

            case AdvancementOutcome.Skipped skipped:
                await publisher.Publish(new ProgressionSkippedEvent(
                    EventRsc: eventRsc,
                    SourceUnitRsc: skipped.SourceUnitRsc,
                    Reason: skipped.Reason,
                    SkippedAt: DateTime.UtcNow), ct);
                break;
        }
    }
}
