namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record UnitResultStartListCreatedEvent(
    string UnitRsc,
    string EventRsc,
    IReadOnlyList<CompetitorSnapshot> Competitors,
    DateTime CreatedAt) : DomainEventBase;

public sealed record CompetitorSnapshot(
    int SortOrder,
    string? ParticipantId,
    int? Seed,
    string Organisation);
