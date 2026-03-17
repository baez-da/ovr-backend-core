using OVR.SharedKernel.Domain.Primitives;

namespace OVR.SharedKernel.Domain.ValueObjects;

/// <summary>
/// Tracks the origin of data from external systems (GMS, Win2tec, LOC, etc.).
/// The System discriminator identifies the source; Value is the ID in that system.
/// </summary>
public sealed class ExternalSystemId : ValueObject
{
    public string System { get; }
    public string Value { get; }

    private ExternalSystemId(string system, string value)
    {
        System = system;
        Value = value;
    }

    public static ExternalSystemId Create(string system, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(system);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new ExternalSystemId(system, value);
    }

    public override string ToString() => $"{System}:{Value}";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return System;
        yield return Value;
    }
}
