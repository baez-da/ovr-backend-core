namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record ProgressionSkippedEvent(
    string EventRsc,
    string SourceUnitRsc,
    string Reason,
    DateTime SkippedAt) : DomainEventBase;
