using System.Collections.Concurrent;
using MediatR;
using OVR.SharedKernel.Domain.Events.Integration;

namespace OVR.Api.IntegrationTests.Progression.Support;

/// <summary>
/// Singleton shared bag that sink handlers write into.
/// Each factory instance gets its own scope via Reset().
/// </summary>
public sealed class CapturedEvents
{
    public ConcurrentQueue<INotification> All { get; } = new();

    public void Reset() { while (All.TryDequeue(out _)) { } }

    public IReadOnlyList<T> OfType<T>() where T : INotification
        => All.OfType<T>().ToList();
}

// --- Sink handlers (one per event type we care about) ---

public sealed class CompetitorAdvancedSink(CapturedEvents bag)
    : INotificationHandler<CompetitorAdvancedEvent>
{
    public Task Handle(CompetitorAdvancedEvent n, CancellationToken ct)
    {
        bag.All.Enqueue(n);
        return Task.CompletedTask;
    }
}

public sealed class ProgressionSkippedSink(CapturedEvents bag)
    : INotificationHandler<ProgressionSkippedEvent>
{
    public Task Handle(ProgressionSkippedEvent n, CancellationToken ct)
    {
        bag.All.Enqueue(n);
        return Task.CompletedTask;
    }
}

public sealed class EventProgressionCompletedSink(CapturedEvents bag)
    : INotificationHandler<EventProgressionCompletedEvent>
{
    public Task Handle(EventProgressionCompletedEvent n, CancellationToken ct)
    {
        bag.All.Enqueue(n);
        return Task.CompletedTask;
    }
}

public sealed class UnitResultOfficialSink(CapturedEvents bag)
    : INotificationHandler<UnitResultOfficialEvent>
{
    public Task Handle(UnitResultOfficialEvent n, CancellationToken ct)
    {
        bag.All.Enqueue(n);
        return Task.CompletedTask;
    }
}

public sealed class UnitResultStartListCreatedSink(CapturedEvents bag)
    : INotificationHandler<UnitResultStartListCreatedEvent>
{
    public Task Handle(UnitResultStartListCreatedEvent n, CancellationToken ct)
    {
        bag.All.Enqueue(n);
        return Task.CompletedTask;
    }
}
