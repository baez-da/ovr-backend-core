using OVR.Modules.Progression.Domain;
using OVR.SharedKernel.Domain.Progression;

namespace OVR.Modules.Progression.Persistence;

internal static class BracketProgressionMapping
{
    public static BracketProgression ToDomain(BracketProgressionDocument doc) =>
        BracketProgression.Hydrate(
            eventRsc: doc.EventRsc,
            edges: doc.Edges.Select(e => new ProgressionEdge(
                e.SourceUnitRsc,
                ParseOutcome(doc.EventRsc, e.Outcome),
                e.TargetUnitRsc,
                e.TargetSlot)).ToList(),
            readyTargets: doc.ReadyTargets,
            pending: doc.PendingAdvancements.Select(p => new PendingAdvancement(
                p.TargetUnitRsc,
                p.TargetSlot,
                p.ParticipantId,
                p.SourceUnitRsc,
                p.RecordedAt)),
            createdAt: doc.CreatedAt);

    public static BracketProgressionDocument ToDocument(BracketProgression agg) => new()
    {
        EventRsc = agg.EventRsc,
        Edges = agg.Edges.Select(e => new ProgressionEdgeDocument
        {
            SourceUnitRsc = e.SourceUnitRsc,
            Outcome = e.Outcome.ToString(),
            TargetUnitRsc = e.TargetUnitRsc,
            TargetSlot = e.TargetSlot
        }).ToList(),
        ReadyTargets = agg.ReadyTargets.ToList(),
        PendingAdvancements = agg.PendingAdvancements.Select(p => new PendingAdvancementDocument
        {
            TargetUnitRsc = p.TargetUnitRsc,
            TargetSlot = p.TargetSlot,
            ParticipantId = p.ParticipantId,
            SourceUnitRsc = p.SourceUnitRsc,
            RecordedAt = p.RecordedAt
        }).ToList(),
        CreatedAt = agg.CreatedAt
    };

    private static Outcome ParseOutcome(string eventRsc, string value)
    {
        if (Enum.TryParse<Outcome>(value, out var parsed))
            return parsed;

        throw new InvalidOperationException(
            $"Unknown Outcome value '{value}' in BracketProgression document for event '{eventRsc}'.");
    }
}
