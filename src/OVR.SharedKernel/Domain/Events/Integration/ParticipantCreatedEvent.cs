namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record ParticipantCreatedEvent(
    string ParticipantId,
    IReadOnlyList<string> MainFunctionIds,
    string? Organisation) : DomainEventBase;
