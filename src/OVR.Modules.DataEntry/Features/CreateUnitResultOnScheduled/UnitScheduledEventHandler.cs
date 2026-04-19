using MediatR;
using Microsoft.Extensions.Logging;
using OVR.Modules.CompetitionConfig.Contracts;
using OVR.Modules.DataEntry.Domain;
using OVR.Modules.DataEntry.Lineup;
using OVR.Modules.DataEntry.Persistence;
using OVR.Modules.Entries.Contracts;
using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.DataEntry.Features.CreateUnitResultOnScheduled;

public sealed class UnitScheduledEventHandler : INotificationHandler<UnitScheduledEvent>
{
    private readonly IUnitResultRepository _repository;
    private readonly IUnitLineupReader _lineupReader;
    private readonly IEntryReader _entryReader;
    private readonly IFirstRoundLineupResolver _resolver;
    private readonly IPublisher _publisher;
    private readonly ILogger<UnitScheduledEventHandler> _logger;

    public UnitScheduledEventHandler(
        IUnitResultRepository repository,
        IUnitLineupReader lineupReader,
        IEntryReader entryReader,
        IFirstRoundLineupResolver resolver,
        IPublisher publisher,
        ILogger<UnitScheduledEventHandler> logger)
    {
        _repository = repository;
        _lineupReader = lineupReader;
        _entryReader = entryReader;
        _resolver = resolver;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Handle(UnitScheduledEvent notification, CancellationToken ct)
    {
        // TD R3 — idempotency
        if (await _repository.ExistsAsync(notification.UnitRsc, ct))
        {
            _logger.LogInformation(
                "UnitResult for {UnitRsc} already exists; skipping.", notification.UnitRsc);
            return;
        }

        var (seedA, seedB) = await _lineupReader.GetSeedsForUnit(notification.UnitRsc, ct);
        if (seedA is null || seedB is null)
        {
            _logger.LogWarning(
                "Unit {UnitRsc} has no seeds assigned; skipping lineup fill.",
                notification.UnitRsc);
            return;
        }

        var activeEntries = await _entryReader.ListActiveByEventRsc(notification.EventRsc, ct);
        var lineupResult = _resolver.Resolve(seedA.Value, seedB.Value, activeEntries);
        if (lineupResult.IsError)
        {
            _logger.LogWarning(
                "Lineup resolution failed for {UnitRsc}: {Error}",
                notification.UnitRsc, lineupResult.FirstError.Description);
            return;
        }

        var (red, blue) = lineupResult.Value;
        var created = UnitResult.CreateForFirstRound(Rsc.Create(notification.UnitRsc), red, blue);
        if (created.IsError)
        {
            _logger.LogWarning(
                "Failed to create UnitResult for {UnitRsc}: {Error}",
                notification.UnitRsc, created.FirstError.Description);
            return;
        }

        var unitResult = created.Value;
        var inserted = await _repository.SaveNewAsync(unitResult, ct);

        if (!inserted)
        {
            _logger.LogInformation(
                "Concurrent create for {UnitRsc} resolved via duplicate-key; skipping event publication.",
                notification.UnitRsc);
            unitResult.ClearDomainEvents();
            return;
        }

        foreach (var domainEvent in unitResult.DomainEvents)
        {
            await _publisher.Publish(domainEvent, ct);
        }
        unitResult.ClearDomainEvents();
    }
}
