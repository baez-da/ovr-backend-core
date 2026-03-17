namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record EntryInscriptionStatusChangedEvent(
    string EntryId,
    string EventRsc,
    string OldStatus,
    string NewStatus) : DomainEventBase;
