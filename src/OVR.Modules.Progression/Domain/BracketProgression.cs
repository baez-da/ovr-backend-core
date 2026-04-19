using ErrorOr;
using OVR.Modules.Progression.Errors;
using OVR.SharedKernel.Domain.Progression;

namespace OVR.Modules.Progression.Domain;

public sealed class BracketProgression
{
    private readonly HashSet<string> _readyTargets;
    private readonly List<PendingAdvancement> _pending;
    private readonly Dictionary<(string Source, Outcome Outcome), ProgressionEdge> _edgeIndex;

    private BracketProgression(
        string eventRsc,
        IReadOnlyList<ProgressionEdge> edges,
        HashSet<string> readyTargets,
        List<PendingAdvancement> pending,
        DateTime createdAt)
    {
        EventRsc = eventRsc;
        Edges = edges;
        _readyTargets = readyTargets;
        _pending = pending;
        _edgeIndex = edges.ToDictionary(e => (e.SourceUnitRsc, e.Outcome));
        CreatedAt = createdAt;
    }

    public string EventRsc { get; }
    public IReadOnlyList<ProgressionEdge> Edges { get; }
    public IReadOnlyCollection<string> ReadyTargets => _readyTargets;
    public IReadOnlyList<PendingAdvancement> PendingAdvancements => _pending;
    public DateTime CreatedAt { get; }

    public static ErrorOr<BracketProgression> Create(
        string eventRsc,
        IEnumerable<ProgressionEdge> edges)
    {
        if (string.IsNullOrWhiteSpace(eventRsc))
            return ProgressionErrors.InvalidEventRsc();

        var edgeList = edges.ToList();

        foreach (var edge in edgeList)
        {
            if (edge.TargetSlot is not 1 and not 2)
                return ProgressionErrors.InvalidSlot(edge.TargetSlot);
        }

        var bySource = new HashSet<(string, Outcome)>();
        foreach (var edge in edgeList)
        {
            if (!bySource.Add((edge.SourceUnitRsc, edge.Outcome)))
                return ProgressionErrors.DuplicateEdge(edge.SourceUnitRsc, edge.Outcome.ToString());
        }

        var byTarget = new HashSet<(string, int)>();
        foreach (var edge in edgeList)
        {
            if (!byTarget.Add((edge.TargetUnitRsc, edge.TargetSlot)))
                return ProgressionErrors.DuplicateTargetSlot(edge.TargetUnitRsc, edge.TargetSlot);
        }

        return new BracketProgression(
            eventRsc,
            edgeList,
            readyTargets: [],
            pending: [],
            createdAt: DateTime.UtcNow);
    }

    public AdvancementOutcome RecordAdvancement(
        string sourceUnitRsc,
        Outcome outcome,
        string? participantId)
    {
        if (string.IsNullOrEmpty(participantId))
            return new AdvancementOutcome.Skipped(sourceUnitRsc, "NoWinner");

        if (!_edgeIndex.TryGetValue((sourceUnitRsc, outcome), out var edge))
            return new AdvancementOutcome.Terminal(sourceUnitRsc, participantId);

        // Buffering invariant: we never emit advancements toward a target whose StartList
        // has not been created. This keeps DataEntry free of "pending/partial UnitResult"
        // state. If future sports need cross-event advancement or lazy target creation,
        // the buffering invariant is where to revisit.
        var alreadyPending = _pending.Any(p =>
            p.SourceUnitRsc == sourceUnitRsc &&
            p.TargetUnitRsc == edge.TargetUnitRsc &&
            p.TargetSlot == edge.TargetSlot &&
            p.ParticipantId == participantId);

        if (_readyTargets.Contains(edge.TargetUnitRsc))
        {
            return new AdvancementOutcome.Ready(edge, participantId);
        }

        if (!alreadyPending)
        {
            _pending.Add(new PendingAdvancement(
                TargetUnitRsc: edge.TargetUnitRsc,
                TargetSlot: edge.TargetSlot,
                ParticipantId: participantId,
                SourceUnitRsc: sourceUnitRsc,
                RecordedAt: DateTime.UtcNow));
        }

        return new AdvancementOutcome.Buffered(edge, participantId);
    }

    public IReadOnlyList<PendingAdvancement> MarkTargetReady(string targetUnitRsc)
    {
        _readyTargets.Add(targetUnitRsc);
        var drained = _pending.Where(p => p.TargetUnitRsc == targetUnitRsc).ToList();
        _pending.RemoveAll(p => p.TargetUnitRsc == targetUnitRsc);
        return drained;
    }

    // Visibility: the mapping class lives in the same assembly, so `internal` suffices.
    internal static BracketProgression Hydrate(
        string eventRsc,
        IReadOnlyList<ProgressionEdge> edges,
        IEnumerable<string> readyTargets,
        IEnumerable<PendingAdvancement> pending,
        DateTime createdAt)
    {
        return new BracketProgression(
            eventRsc,
            edges,
            readyTargets: [..readyTargets],
            pending: [..pending],
            createdAt: createdAt);
    }
}
