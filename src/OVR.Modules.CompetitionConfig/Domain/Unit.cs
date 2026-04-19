using OVR.SharedKernel.Domain.Primitives;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.CompetitionConfig.Domain;

public sealed class Unit : AggregateRoot<string>
{
    public Rsc Rsc { get; private set; } = null!;
    public Rsc EventRsc { get; private set; } = null!;
    public string PhaseCode { get; private set; } = string.Empty;
    public int UnitNumber { get; private set; }
    public int? SeedA { get; private set; }
    public int? SeedB { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Unit() { }

    public static Unit Create(Rsc rsc, int? seedA = null, int? seedB = null)
    {
        ArgumentNullException.ThrowIfNull(rsc);

        if (!rsc.IsAtLevel(RscLevel.Unit))
            throw new ArgumentException(
                $"RSC must be at Unit level, got {rsc.Level}: '{rsc.Value}'.",
                nameof(rsc));

        if ((seedA is null) != (seedB is null))
            throw new ArgumentException("SeedA and SeedB must both be null or both be set.");

        var eventRsc = Rsc.Create(rsc.AtEventLevel());
        var unitNumberStr = rsc.Unit.TrimEnd('-');
        var unitNumber = int.Parse(unitNumberStr);

        return new Unit
        {
            Id = rsc.Value,
            Rsc = rsc,
            EventRsc = eventRsc,
            PhaseCode = rsc.Phase,
            UnitNumber = unitNumber,
            SeedA = seedA,
            SeedB = seedB,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Unit Hydrate(Rsc rsc, int? seedA, int? seedB, DateTime createdAt)
    {
        ArgumentNullException.ThrowIfNull(rsc);

        if (!rsc.IsAtLevel(RscLevel.Unit))
            throw new ArgumentException(
                $"RSC must be at Unit level, got {rsc.Level}: '{rsc.Value}'.",
                nameof(rsc));

        if ((seedA is null) != (seedB is null))
            throw new ArgumentException("SeedA and SeedB must both be null or both be set.");

        var eventRsc = Rsc.Create(rsc.AtEventLevel());
        var unitNumberStr = rsc.Unit.TrimEnd('-');
        var unitNumber = int.Parse(unitNumberStr);

        return new Unit
        {
            Id = rsc.Value,
            Rsc = rsc,
            EventRsc = eventRsc,
            PhaseCode = rsc.Phase,
            UnitNumber = unitNumber,
            SeedA = seedA,
            SeedB = seedB,
            CreatedAt = createdAt
        };
    }
}
