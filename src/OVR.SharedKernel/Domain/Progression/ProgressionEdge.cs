namespace OVR.SharedKernel.Domain.Progression;

public sealed record ProgressionEdge(
    string SourceUnitRsc,
    Outcome Outcome,
    string TargetUnitRsc,
    int TargetSlot);
