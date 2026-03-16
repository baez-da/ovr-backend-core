using MediatR;

namespace OVR.SharedKernel.Domain.Events;

public interface IDomainEvent : INotification
{
    DateTime OccurredOn { get; }
}

public abstract record DomainEventBase : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
