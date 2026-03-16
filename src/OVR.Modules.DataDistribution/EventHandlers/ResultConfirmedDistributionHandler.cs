using MediatR;
using Microsoft.Extensions.Logging;
using OVR.SharedKernel.Domain.Events.Integration;

namespace OVR.Modules.DataDistribution.EventHandlers;

public sealed class ResultConfirmedDistributionHandler(ILogger<ResultConfirmedDistributionHandler> logger)
    : INotificationHandler<ResultConfirmedEvent>
{
    public Task Handle(ResultConfirmedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "DataDistribution: Queuing ODF message for confirmed result on unit {UnitRsc}",
            notification.UnitRsc);

        // TODO: Enqueue ODF message to outbox for delivery
        return Task.CompletedTask;
    }
}
