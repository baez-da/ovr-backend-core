namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record ParticipantCreatedEvent(
    string ParticipantId,
    string ParticipantType,
    string? Organisation) : DomainEventBase;
