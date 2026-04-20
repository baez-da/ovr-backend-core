namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record CompetitorAdvancedEvent(
    string EventRsc,
    string TargetUnitRsc,
    int TargetSlot,
    string ParticipantId,
    string SourceUnitRsc,
    DateTime AdvancedAt) : DomainEventBase;
