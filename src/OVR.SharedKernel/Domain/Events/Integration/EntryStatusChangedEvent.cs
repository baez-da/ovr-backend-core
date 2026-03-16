namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record EntryStatusChangedEvent(
    string EntryId,
    string EventRsc,
    string OldStatus,
    string NewStatus) : DomainEventBase;
