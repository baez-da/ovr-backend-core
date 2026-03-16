namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record ResultConfirmedEvent(
    string UnitRsc,
    string Status,
    DateTime ConfirmedAt) : DomainEventBase;
