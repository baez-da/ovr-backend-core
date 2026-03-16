using OVR.SharedKernel.Domain.Primitives;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.Scheduling.Domain;

public sealed class Unit : AggregateRoot<string>
{
    public Rsc UnitRsc { get; private set; } = null!;
    public UnitStatus Status { get; private set; } = UnitStatus.Scheduled;
    public DateTime? StartTime { get; private set; }
    public string? Venue { get; private set; }

    private Unit() { }

    public static Unit Create(Rsc unitRsc, DateTime? startTime, string? venue)
    {
        return new Unit
        {
            Id = unitRsc.Value,
            UnitRsc = unitRsc,
            Status = UnitStatus.Scheduled,
            StartTime = startTime,
            Venue = venue
        };
    }
}
