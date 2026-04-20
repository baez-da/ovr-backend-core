using MediatR;
using Microsoft.Extensions.Logging;
using OVR.Modules.DataEntry.Persistence;
using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.DataEntry.EventHandlers;

public sealed class CompetitorAdvancedHandler(
    IUnitResultRepository repository,
    ILogger<CompetitorAdvancedHandler> logger)
    : INotificationHandler<CompetitorAdvancedEvent>
{
    public async Task Handle(CompetitorAdvancedEvent notification, CancellationToken ct)
    {
        var ur = await repository.GetAsync(notification.TargetUnitRsc, ct);
        if (ur is null)
        {
            logger.LogError(
                "UnitResult not found for target {TargetUnitRsc} — CompetitorAdvanced cannot apply.",
                notification.TargetUnitRsc);
            return;
        }

        var participantId = ParticipantId.Create(notification.ParticipantId);
        var result = ur.AdvanceCompetitor(notification.TargetSlot, participantId);
        if (result.IsError)
        {
            logger.LogWarning(
                "AdvanceCompetitor on {TargetUnitRsc}/{Slot} returned {ErrorCode}: {Description}",
                notification.TargetUnitRsc,
                notification.TargetSlot,
                result.FirstError.Code,
                result.FirstError.Description);
            return;
        }

        await repository.UpdateAsync(ur, ct);
    }
}
