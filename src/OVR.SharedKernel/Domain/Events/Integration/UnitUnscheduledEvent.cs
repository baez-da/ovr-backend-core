namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record UnitUnscheduledEvent(
    string UnitRsc,
    string EventRsc,
    DateTime UnscheduledAt) : DomainEventBase;
