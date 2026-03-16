using OVR.SharedKernel.Domain.Primitives;

namespace OVR.SharedKernel.Domain.ValueObjects;

public sealed class Gender : ValueObject
{
    public static readonly Gender Male = new("M");
    public static readonly Gender Female = new("F");
    public static readonly Gender Mixed = new("X");

    public string Value { get; }

    private Gender(string value) => Value = value;

    public static Gender FromCode(string code) => code.ToUpperInvariant() switch
    {
        "M" => Male,
        "F" => Female,
        "X" => Mixed,
        _ => throw new ArgumentException($"Invalid gender code: '{code}'. Valid values are M, F, X.", nameof(code))
    };

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
