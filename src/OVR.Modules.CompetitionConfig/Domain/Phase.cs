using OVR.SharedKernel.Domain.Primitives;

namespace OVR.Modules.CompetitionConfig.Domain;

public sealed class Phase : Entity<string>
{
    public string Code { get; private set; } = string.Empty;
    public int Order { get; private set; }
    public int UnitCount { get; private set; }

    private Phase() { }

    // Public for tests; intended for internal use by Event aggregate only.
    public static Phase CreateInternal(string code, int order, int unitCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentOutOfRangeException.ThrowIfNegative(order);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(unitCount);

        return new Phase { Id = code, Code = code, Order = order, UnitCount = unitCount };
    }
}
