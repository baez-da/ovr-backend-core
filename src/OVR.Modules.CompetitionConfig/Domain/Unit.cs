using OVR.SharedKernel.Domain.Primitives;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.CompetitionConfig.Domain;

public sealed class Unit : AggregateRoot<string>
{
    public Rsc Rsc { get; private set; } = null!;
    public Rsc EventRsc { get; private set; } = null!;
    public string PhaseCode { get; private set; } = string.Empty;
    public int UnitNumber { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Unit() { }

    public static Unit Create(Rsc rsc)
    {
        ArgumentNullException.ThrowIfNull(rsc);

        if (!rsc.IsAtLevel(RscLevel.Unit))
            throw new ArgumentException(
                $"RSC must be at Unit level, got {rsc.Level}: '{rsc.Value}'.",
                nameof(rsc));

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
            CreatedAt = DateTime.UtcNow
        };
    }
}
