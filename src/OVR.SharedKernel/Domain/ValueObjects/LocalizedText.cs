using OVR.SharedKernel.Domain.Primitives;

namespace OVR.SharedKernel.Domain.ValueObjects;

public sealed class LocalizedText : ValueObject
{
    public string Long { get; }
    public string? Short { get; }

    private LocalizedText(string @long, string? @short)
    {
        Long = @long;
        Short = @short;
    }

    public static LocalizedText Create(string @long, string? @short = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(@long);
        return new LocalizedText(@long.Trim(), @short?.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Long;
        yield return Short;
    }
}
