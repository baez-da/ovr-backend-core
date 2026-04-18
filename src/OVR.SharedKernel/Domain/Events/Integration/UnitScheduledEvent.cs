namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record UnitScheduledEvent(
    string UnitRsc,
    string EventRsc,
    string SessionCode,
    string LocationCode,
    DateTime StartTime,
    int OrderInSession,
    int OrderInLocation,
    DateTime ScheduledAt) : DomainEventBase;
