using System.Text;

namespace OVR.Modules.ParticipantRegistry.Domain.NameSystem;

public static class NameNormalization
{
    // ODF character table: maps characters that cannot be expressed in ASCII
    // to their ASCII equivalents, preserving case where applicable.
    private static readonly Dictionary<char, string> AsciiMap = new()
    {
        // Special ligatures and eth/thorn
        { 'Æ', "AE" }, { 'æ', "ae" },
        { 'Þ', "TH" }, { 'þ', "th" },
        { 'ß', "ss" }, // always lowercase per ODF

        // N with tilde
        { 'Ñ', "N" }, { 'ñ', "n" },

        // A variants
        { 'À', "A" }, { 'Á', "A" }, { 'Â', "A" }, { 'Ã', "A" }, { 'Ä', "A" }, { 'Å', "A" },
        { 'à', "a" }, { 'á', "a" }, { 'â', "a" }, { 'ã', "a" }, { 'ä', "a" }, { 'å', "a" },

        // C variants
        { 'Ç', "C" }, { 'ç', "c" },

        // E variants
        { 'È', "E" }, { 'É', "E" }, { 'Ê', "E" }, { 'Ë', "E" },
        { 'è', "e" }, { 'é', "e" }, { 'ê', "e" }, { 'ë', "e" },

        // I variants
        { 'Ì', "I" }, { 'Í', "I" }, { 'Î', "I" }, { 'Ï', "I" },
        { 'ì', "i" }, { 'í', "i" }, { 'î', "i" }, { 'ï', "i" },

        // O variants
        { 'Ò', "O" }, { 'Ó', "O" }, { 'Ô', "O" }, { 'Õ', "O" }, { 'Ö', "O" }, { 'Ø', "O" },
        { 'ò', "o" }, { 'ó', "o" }, { 'ô', "o" }, { 'õ', "o" }, { 'ö', "o" }, { 'ø', "o" },

        // U variants
        { 'Ù', "U" }, { 'Ú', "U" }, { 'Û', "U" }, { 'Ü', "U" },
        { 'ù', "u" }, { 'ú', "u" }, { 'û', "u" }, { 'ü', "u" },

        // Y variants
        { 'Ý', "Y" }, { 'ý', "y" }, { 'ÿ', "y" },
    };

    /// <summary>
    /// Converts accented and special characters to their ASCII equivalents
    /// per the ODF character table.
    /// </summary>
    public static string ToAscii(string name)
    {
        if (name.Length == 0)
            return name;

        var sb = new StringBuilder(name.Length);
        foreach (var ch in name)
        {
            if (AsciiMap.TryGetValue(ch, out var replacement))
                sb.Append(replacement);
            else
                sb.Append(ch);
        }
        return sb.ToString();
    }

    private static readonly HashSet<string> Particles = new(StringComparer.OrdinalIgnoreCase)
    {
        "da", "de", "dei", "del", "den", "der", "di", "dos",
        "du", "la", "le", "los", "ter", "van", "vander", "von"
    };

    /// <summary>
    /// Applies ODF Section 6.2 "limited mixed case" rules for FamilyName.
    /// Particles → lowercase; Mc prefix → McUPPER; everything else → UPPERCASE.
    /// </summary>
    public static string ToLimitedMixedCase(string name)
    {
        if (name.Length == 0)
            return name;

        var spaceParts = name.Split(' ');
        var resultParts = new List<string>(spaceParts.Length);

        foreach (var spacePart in spaceParts)
        {
            var hyphenParts = spacePart.Split('-');
            var hyphenResults = new List<string>(hyphenParts.Length);

            foreach (var word in hyphenParts)
            {
                hyphenResults.Add(ApplyLimitedMixedCaseWord(word));
            }

            resultParts.Add(string.Join("-", hyphenResults));
        }

        return string.Join(" ", resultParts);
    }

    private static string ApplyLimitedMixedCaseWord(string word)
    {
        if (word.Length == 0)
            return word;

        if (Particles.Contains(word))
            return word.ToLowerInvariant();

        if (word.Length >= 3 && word[..2].Equals("mc", StringComparison.OrdinalIgnoreCase))
            return "Mc" + word[2..].ToUpperInvariant();

        return word.ToUpperInvariant();
    }
}
