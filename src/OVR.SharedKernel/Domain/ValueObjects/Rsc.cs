using System.Text.RegularExpressions;
using OVR.SharedKernel.Domain.Primitives;

namespace OVR.SharedKernel.Domain.ValueObjects;

public enum RscLevel
{
    Discipline,
    Event,
    Phase,
    Unit,
    SubUnit
}

public sealed partial class Rsc : ValueObject
{
    public const int FullLength = 34;
    private const string Pad = "-";

    public string Value { get; }

    private Rsc(string value) => Value = value;

    public static Rsc Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalized = value.Length < FullLength
            ? value.PadRight(FullLength, '-')
            : value;

        if (normalized.Length != FullLength)
            throw new ArgumentException(
                $"RSC must be {FullLength} characters after padding, got {normalized.Length}: '{value}'.",
                nameof(value));

        if (!RscPattern().IsMatch(normalized))
            throw new ArgumentException(
                $"Invalid RSC format: '{value}'. Expected 3 uppercase discipline chars followed by gender and segments.",
                nameof(value));

        return new Rsc(normalized);
    }

    // Parsed segments
    public string Discipline => Value[..3];
    public char Gender => Value[3];
    public string Event => Value[4..22];
    public string Phase => Value[22..26];
    public string UnitBlock => Value[26..34];
    public string Unit => Value[26..32];
    public string SubUnit => Value[32..34];

    // Level detection
    public RscLevel Level
    {
        get
        {
            if (IsAllDashes(Value[4..]))
                return RscLevel.Discipline;
            if (IsAllDashes(Value[22..]))
                return RscLevel.Event;
            if (IsAllDashes(Value[26..]))
                return RscLevel.Phase;
            if (SubUnit is "--" or "00")
                return RscLevel.Unit;
            return RscLevel.SubUnit;
        }
    }

    public bool HasSubUnit => SubUnit is not "--" and not "00";
    public bool IsAtLevel(RscLevel expected) => Level == expected;
    public bool IsAtLeastLevel(RscLevel minimum) => Level >= minimum;

    // Truncate to a specific level
    public string AtDisciplineLevel() => Value[..3].PadRight(FullLength, '-');
    public string AtEventLevel() => Value[..22].PadRight(FullLength, '-');
    public string AtPhaseLevel() => Value[..26].PadRight(FullLength, '-');
    public string AtUnitLevel() => Value[..32] + "--";

    // For hierarchical prefix queries
    public string PrefixForLevel(RscLevel level) => level switch
    {
        RscLevel.Discipline => Value[..3],
        RscLevel.Event => Value[..22].TrimEnd('-'),
        RscLevel.Phase => Value[..26].TrimEnd('-'),
        RscLevel.Unit => Value[..32].TrimEnd('-'),
        RscLevel.SubUnit => Value.TrimEnd('-'),
        _ => Value
    };

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    private static bool IsAllDashes(string segment) =>
        segment.All(c => c == '-');

    [GeneratedRegex(@"^[A-Z]{3}[A-Z0-9].{30}$")]
    private static partial Regex RscPattern();
}
