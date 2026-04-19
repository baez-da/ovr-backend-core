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
}
