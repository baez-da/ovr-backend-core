using OVR.SharedKernel.Domain.Primitives;

namespace OVR.SharedKernel.Domain.ValueObjects;

public sealed class Noc : ValueObject
{
    public string Code { get; }

    private Noc(string code) => Code = code;

    public static Noc Create(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        if (code.Length != 3 || !code.All(char.IsUpper))
            throw new ArgumentException($"NOC must be exactly 3 uppercase letters, got '{code}'.", nameof(code));
        return new Noc(code);
    }

    public override string ToString() => Code;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
    }
}
