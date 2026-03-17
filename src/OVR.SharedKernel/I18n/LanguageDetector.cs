using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace OVR.SharedKernel.I18n;

public static partial class LanguageDetector
{
    public const string DefaultLanguage = "eng";

    private static readonly Dictionary<string, string> Iso639Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = "eng",
        ["es"] = "spa",
        ["pt"] = "por",
        ["de"] = "deu",
        ["fr"] = "fra",
        ["it"] = "ita",
        ["ja"] = "jpn",
        ["ko"] = "kor",
        ["zh"] = "chi",
        ["ru"] = "rus",
        ["ar"] = "ara",
        ["hi"] = "hin",
    };

    private static readonly HashSet<string> SupportedLanguages =
        new(["eng", "spa", "por"], StringComparer.OrdinalIgnoreCase);

    public static string DetectLanguage(HttpContext httpContext)
    {
        // Priority 1: Custom "Language" header
        var languageHeader = httpContext.Request.Headers["Language"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(languageHeader))
        {
            var normalized = NormalizeLanguageCode(languageHeader.Trim());
            if (SupportedLanguages.Contains(normalized))
                return normalized;
        }

        // Priority 2: Standard "Accept-Language" header
        var acceptLanguage = httpContext.Request.Headers.AcceptLanguage.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(acceptLanguage))
        {
            var primaryLang = AcceptLanguageRegex().Match(acceptLanguage).Groups[1].Value;
            if (!string.IsNullOrEmpty(primaryLang))
            {
                var normalized = NormalizeLanguageCode(primaryLang);
                if (SupportedLanguages.Contains(normalized))
                    return normalized;
            }
        }

        return DefaultLanguage;
    }

    private static string NormalizeLanguageCode(string code)
    {
        var baseCode = code.Split('-')[0].ToLowerInvariant();

        if (SupportedLanguages.Contains(baseCode))
            return baseCode;

        return Iso639Map.GetValueOrDefault(baseCode, DefaultLanguage);
    }

    [GeneratedRegex(@"^([a-zA-Z]{2,3})")]
    private static partial Regex AcceptLanguageRegex();
}
