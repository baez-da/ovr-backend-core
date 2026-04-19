using MediatR;
using Microsoft.Extensions.Logging;
using OVR.Modules.Progression.Domain;
using OVR.Modules.Progression.Persistence;
using OVR.SharedKernel.Domain.Events.Integration;

namespace OVR.Modules.Progression.EventHandlers;

public sealed class EventStructureGeneratedHandler(
    IBracketProgressionRepository repository,
    ILogger<EventStructureGeneratedHandler> logger)
    : INotificationHandler<EventStructureGeneratedEvent>
{
    public async Task Handle(EventStructureGeneratedEvent notification, CancellationToken ct)
    {
        var existing = await repository.GetByEventAsync(notification.EventRsc, ct);
        if (existing is not null)
        {
            logger.LogInformation(
                "BracketProgression already exists for event {EventRsc} — skipping.",
                notification.EventRsc);
            return;
        }

        var result = BracketProgression.Create(notification.EventRsc, notification.Edges);
        if (result.IsError)
        {
            logger.LogError(
                "Failed to create BracketProgression for event {EventRsc}: {ErrorCode} {Description}",
                notification.EventRsc, result.FirstError.Code, result.FirstError.Description);
            return;
        }

        var inserted = await repository.SaveNewAsync(result.Value, ct);
        if (!inserted)
        {
            logger.LogInformation(
                "BracketProgression insertion for event {EventRsc} lost idempotency race — no-op.",
                notification.EventRsc);
        }
    }
}
