using OVR.SharedKernel.Domain.Primitives;

namespace OVR.SharedKernel.Domain.ValueObjects;

/// <summary>
/// Represents the competing entity: NOC (Olympics), club, district, university, etc.
/// ODF uses "Organisation" as the generic term. Always 3 uppercase characters.
/// </summary>
public sealed class Organisation : ValueObject
{
    public string Code { get; }

    private Organisation(string code) => Code = code;

    public static Organisation Create(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        if (code.Length != 3 || !code.All(char.IsUpper))
            throw new ArgumentException(
                $"Organisation code must be exactly 3 uppercase letters, got '{code}'.",
                nameof(code));
        return new Organisation(code);
    }

    public override string ToString() => Code;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
    }
}
