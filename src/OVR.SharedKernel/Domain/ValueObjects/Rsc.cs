using System.Text.RegularExpressions;
using OVR.SharedKernel.Domain.Primitives;

namespace OVR.SharedKernel.Domain.ValueObjects;

public sealed partial class Rsc : ValueObject
{
    public string Value { get; }

    private Rsc(string value) => Value = value;

    public static Rsc Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (!RscPattern().IsMatch(value))
            throw new ArgumentException($"Invalid RSC format: '{value}'. Expected pattern like 'SWMW400MFR01'.", nameof(value));
        return new Rsc(value);
    }

    public string Discipline => Value[..3];
    public char Gender => Value[3];
    public string EventCode => Value[4..];

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    [GeneratedRegex(@"^[A-Z]{3}[A-Z0-9].{4,}$")]
    private static partial Regex RscPattern();
}
