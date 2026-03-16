namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record UnitScheduleChangedEvent(
    string UnitRsc,
    DateTime? NewStartTime,
    string Reason) : DomainEventBase;
