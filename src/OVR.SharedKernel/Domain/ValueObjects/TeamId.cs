using OVR.SharedKernel.Domain.Primitives;

namespace OVR.SharedKernel.Domain.ValueObjects;

public sealed class TeamId : ValueObject
{
    public string Value { get; }

    private TeamId(string value) => Value = value;

    public static TeamId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new TeamId(value);
    }

    public string? ExtractNoc() => Value.Length >= 16 ? Value[13..16] : null;

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
