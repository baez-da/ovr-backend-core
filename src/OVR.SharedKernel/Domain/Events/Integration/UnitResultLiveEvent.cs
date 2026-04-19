namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record UnitResultLiveEvent(
    string UnitRsc,
    DateTime StartedAt) : DomainEventBase;
