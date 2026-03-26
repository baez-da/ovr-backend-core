namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record ParticipantCreatedEvent(
    string ParticipantId,
    IReadOnlyList<string> MainFunctions,
    string? Organisation) : DomainEventBase;
