namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record EntryCreatedEvent(
    string EntryId,
    string ParticipantId,
    string EventRsc,
    string CompetitorType,
    string OrganisationCode) : DomainEventBase;
