using MediatR;
using Microsoft.Extensions.Logging;
using OVR.SharedKernel.Domain.Events.Integration;

namespace OVR.Modules.Progression.EventHandlers;

public sealed class ResultConfirmedHandler(ILogger<ResultConfirmedHandler> logger)
    : INotificationHandler<ResultConfirmedEvent>
{
    public Task Handle(ResultConfirmedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Progression: Processing confirmed result for unit {UnitRsc} with status {Status}",
            notification.UnitRsc,
            notification.Status);

        // TODO: Implement progression logic - determine which participants advance
        return Task.CompletedTask;
    }
}
