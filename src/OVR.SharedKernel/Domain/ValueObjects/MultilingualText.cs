using OVR.SharedKernel.Domain.Primitives;

namespace OVR.SharedKernel.Domain.ValueObjects;

public sealed class MultilingualText : ValueObject
{
    private readonly Dictionary<string, LocalizedText> _translations;

    public IReadOnlyDictionary<string, LocalizedText> All => _translations;

    private MultilingualText(Dictionary<string, LocalizedText> translations)
    {
        _translations = translations;
    }

    public static MultilingualText Create(Dictionary<string, LocalizedText> translations)
    {
        if (translations is null || translations.Count == 0)
            throw new ArgumentException("At least one translation is required.", nameof(translations));

        var normalized = translations.ToDictionary(
            kvp => kvp.Key.ToLowerInvariant(),
            kvp => kvp.Value);
        return new MultilingualText(normalized);
    }

    public LocalizedText? GetOrDefault(string language)
        => _translations.GetValueOrDefault(language.ToLowerInvariant());

    public LocalizedText Resolve(string language, string fallback = "eng")
        => GetOrDefault(language) ?? GetOrDefault(fallback)
           ?? _translations.Values.First();

    public MultilingualText Filter(IEnumerable<string> languages)
    {
        var filtered = languages
            .Select(l => l.ToLowerInvariant())
            .Where(l => _translations.ContainsKey(l))
            .ToDictionary(l => l, l => _translations[l]);
        return filtered.Count > 0 ? new MultilingualText(filtered) : this;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        foreach (var kvp in _translations.OrderBy(x => x.Key))
        {
            yield return kvp.Key;
            yield return kvp.Value;
        }
    }
}
