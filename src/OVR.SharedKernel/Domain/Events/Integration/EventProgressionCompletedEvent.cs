namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record EventProgressionCompletedEvent(
    string EventRsc,
    string FinalUnitRsc,
    string ChampionParticipantId,
    DateTime CompletedAt) : DomainEventBase;
