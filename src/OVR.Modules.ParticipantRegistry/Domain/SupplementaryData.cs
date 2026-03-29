namespace OVR.Modules.ParticipantRegistry.Domain;

public sealed class SupplementaryData
{
    private readonly Dictionary<string, string> _properties = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> Properties => _properties;

    public void Set(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _properties[key] = value;
    }

    public string? Get(string key) => _properties.GetValueOrDefault(key);
}
