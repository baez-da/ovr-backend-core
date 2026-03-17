using OVR.SharedKernel.Domain.Primitives;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.DataEntry.Domain;

public sealed class CompetitionResult : AggregateRoot<string>
{
    public Rsc UnitRsc { get; private set; } = null!;
    public ResultStatus Status { get; private set; } = ResultStatus.StartList;
    public DateTime? StartedAt { get; private set; }
    public DateTime? FinishedAt { get; private set; }

    private CompetitionResult() { }

    public static CompetitionResult Create(Rsc unitRsc)
    {
        return new CompetitionResult
        {
            Id = unitRsc.Value,
            UnitRsc = unitRsc,
            Status = ResultStatus.StartList
        };
    }

    public void TransitionTo(ResultStatus newStatus)
    {
        // Validate state transitions. It may not be necessary to validate state transitions strictly, but it's important to track them.
        var valid = (Status, newStatus) switch
        {
            (ResultStatus.StartList, ResultStatus.Live) => true,
            (ResultStatus.Live, ResultStatus.Unofficial) => true,
            (ResultStatus.Unofficial, ResultStatus.Official) => true,
            (ResultStatus.Unofficial, ResultStatus.Live) => true, // Revert
            _ => false
        };

        if (!valid)
            throw new InvalidOperationException($"Cannot transition from {Status} to {newStatus}");

        Status = newStatus;

        if (newStatus == ResultStatus.Live)
            StartedAt = DateTime.UtcNow;
        if (newStatus == ResultStatus.Official)
            FinishedAt = DateTime.UtcNow;
    }
}
