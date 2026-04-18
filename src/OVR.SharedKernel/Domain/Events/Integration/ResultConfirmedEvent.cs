namespace OVR.SharedKernel.Domain.Events.Integration;

[Obsolete("Use UnitResultOfficialEvent instead. Will be removed once all consumers are migrated.")]
public sealed record ResultConfirmedEvent(
    string UnitRsc,
    string Status,
    DateTime ConfirmedAt) : DomainEventBase;
