namespace OVR.Modules.Progression.Domain;

public sealed record PendingAdvancement(
    string TargetUnitRsc,
    int TargetSlot,
    string ParticipantId,
    string SourceUnitRsc,
    DateTime RecordedAt);
