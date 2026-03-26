using OVR.SharedKernel.Domain.Primitives;

namespace OVR.SharedKernel.Domain.ValueObjects;

public sealed class ParticipantId : ValueObject
{
    public string Value { get; }

    private ParticipantId(string value) => Value = value;

    public static ParticipantId Generate() => new($"{Guid.NewGuid()}");

    public static ParticipantId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new ParticipantId(value);
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
