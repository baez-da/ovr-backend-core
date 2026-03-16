using OVR.SharedKernel.Domain.Primitives;

namespace OVR.SharedKernel.Abstractions;

public interface IDomainEventDispatcher
{
    Task DispatchEventsAsync<TId>(AggregateRoot<TId> aggregate, CancellationToken ct = default) where TId : notnull;
}
