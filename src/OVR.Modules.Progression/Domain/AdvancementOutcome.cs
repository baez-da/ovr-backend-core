using OVR.SharedKernel.Domain.Progression;

namespace OVR.Modules.Progression.Domain;

public abstract record AdvancementOutcome
{
    public sealed record Ready(ProgressionEdge Edge, string ParticipantId) : AdvancementOutcome;
    public sealed record Buffered(ProgressionEdge Edge, string ParticipantId) : AdvancementOutcome;
    public sealed record Terminal(string SourceUnitRsc, string? ChampionParticipantId) : AdvancementOutcome;
    public sealed record Skipped(string SourceUnitRsc, string Reason) : AdvancementOutcome;
}
