using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace OVR.SharedKernel.I18n;

public sealed partial class JsonTranslationService : ITranslationService
{
    private readonly Dictionary<string, Dictionary<string, string>> _translations = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<JsonTranslationService> _logger;

    public JsonTranslationService(string basePath, ILogger<JsonTranslationService> logger)
    {
        _logger = logger;
        LoadTranslations(basePath);
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
                if (dict is not null)
                {
                    _translations[lang] = new Dictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase);
                    _logger.LogInformation("Loaded {Count} translations for language '{Lang}'", dict.Count, lang);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load translations from {File}", file);
            }
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
