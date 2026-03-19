using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace OVR.SharedKernel.I18n;

public sealed partial class JsonTranslationService : ITranslationService
{
    private readonly Dictionary<string, Dictionary<string, string>> _translations = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<JsonTranslationService> _logger;

    public JsonTranslationService(IEnumerable<string> translationPaths, ILogger<JsonTranslationService> logger)
    {
        _logger = logger;

        foreach (var path in translationPaths)
            LoadTranslations(path);

        DetectMissingLanguages();
    }

    public string Translate(string errorCode, string language, string fallback,
        IDictionary<string, object>? parameters = null)
    {
        // Try requested language
        if (_translations.TryGetValue(language, out var langDict) &&
            langDict.TryGetValue(errorCode, out var template))
        {
            return Interpolate(template, parameters);
        }

        // Fallback to English
        if (language != LanguageDetector.DefaultLanguage &&
            _translations.TryGetValue(LanguageDetector.DefaultLanguage, out var engDict) &&
            engDict.TryGetValue(errorCode, out var engTemplate))
        {
            return Interpolate(engTemplate, parameters);
        }

        // Fallback to original description
        return fallback;
    }

    public string TranslateFieldName(string propertyName, string language)
    {
        var key = $"Fields.{propertyName}";

        if (_translations.TryGetValue(language, out var langDict) &&
            langDict.TryGetValue(key, out var translated))
            return translated;

        if (language != LanguageDetector.DefaultLanguage &&
            _translations.TryGetValue(LanguageDetector.DefaultLanguage, out var engDict) &&
            engDict.TryGetValue(key, out var engTranslated))
            return engTranslated;

        return propertyName;
    }

    private void LoadTranslations(string basePath)
    {
        if (!Directory.Exists(basePath))
        {
            _logger.LogWarning("Translation directory not found: {Path}", basePath);
            return;
        }

        foreach (var file in Directory.GetFiles(basePath, "*.json"))
        {
            var lang = Path.GetFileNameWithoutExtension(file);
            try
            {
                var json = File.ReadAllText(file);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (dict is null)
                    continue;

                if (!_translations.TryGetValue(lang, out var langDict))
                {
                    langDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    _translations[lang] = langDict;
                }

                foreach (var (key, value) in dict)
                {
                    if (!langDict.TryAdd(key, value))
                        _logger.LogWarning(
                            "Duplicate translation key '{Key}' for language '{Lang}' in {File} — keeping first value",
                            key, lang, file);
                }

                _logger.LogInformation("Loaded {Count} translations for language '{Lang}' from {Path}",
                    dict.Count, lang, basePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load translations from {File}", file);
            }
        }
    }

    private void DetectMissingLanguages()
    {
        if (_translations.Count <= 1)
            return;

        var allKeys = _translations
            .SelectMany(t => t.Value.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var (lang, dict) in _translations)
        {
            var missing = allKeys.Where(k => !dict.ContainsKey(k)).ToList();
            if (missing.Count > 0)
                _logger.LogWarning(
                    "Language '{Lang}' is missing {Count} translation(s): {Keys}",
                    lang, missing.Count, string.Join(", ", missing));
        }
    }

    private static string Interpolate(string template, IDictionary<string, object>? parameters)
    {
        if (parameters is null || parameters.Count == 0)
            return template;

        return InterpolationRegex().Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            return parameters.TryGetValue(key, out var value) ? value.ToString() ?? match.Value : match.Value;
        });
    }

    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex InterpolationRegex();
}
