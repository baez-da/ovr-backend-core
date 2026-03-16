namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record ProgressionCompletedEvent(
    string SourceUnitRsc,
    string TargetUnitRsc,
    int AdvancedCount) : DomainEventBase;
