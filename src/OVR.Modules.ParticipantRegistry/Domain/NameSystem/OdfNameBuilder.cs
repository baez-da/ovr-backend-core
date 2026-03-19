namespace OVR.Modules.ParticipantRegistry.Domain.NameSystem;

public sealed class OdfNameBuilder : INameBuilder
{
    private const int MaxPrintName = 35;
    private const int MaxPrintInitialName = 18;
    private const int MaxTvName = 35;
    private const int MaxTvInitialName = 18;
    private const int MaxTvFamilyName = 18;
    private const int MaxPscb = 50;

    private static readonly HashSet<string> AsianNocs = new(StringComparer.OrdinalIgnoreCase)
    {
        "CHN", "JPN", "KOR", "PRK", "TPE", "HKG", "MAC", "COR"
    };

    // ── Public API ──────────────────────────────────────────────────────────

    public string BuildPrintName(string familyName, string? givenName)
    {
        var family = NormalizeFamily(familyName);
        if (givenName is null)
            return Fit(family, MaxPrintName);

        var given = NormalizeGiven(givenName);
        return FitWithGiven(family, given, MaxPrintName);
    }

    public string BuildPrintInitialName(string familyName, string? givenName)
    {
        var family = NormalizeFamily(familyName);
        if (givenName is null)
            return Fit(family, MaxPrintInitialName);

        var givenAscii = NameNormalization.ToAscii(givenName);
        var initials = GetInitials(givenAscii); // uppercase, no dot
        return FitWithInitials(family, initials, MaxPrintInitialName, addDotToInitial: false);
    }

    public string BuildTvName(string familyName, string? givenName, string? organisationCode = null)
    {
        var family = NormalizeFamily(familyName);
        if (givenName is null)
            return Fit(family, MaxTvName);

        var given = NormalizeGiven(givenName);
        bool asian = organisationCode is not null && AsianNocs.Contains(organisationCode);

        // Asian: {Family} {Given}; Western: {Given} {Family}
        return asian
            ? FitAsian(family, given, MaxTvName)
            : FitWithGivenWestern(family, given, MaxTvName);
    }

    public string BuildTvInitialName(string familyName, string? givenName, string? organisationCode = null)
    {
        var family = NormalizeFamily(familyName);
        if (givenName is null)
            return Fit(family, MaxTvInitialName);

        var givenAscii = NameNormalization.ToAscii(givenName);
        var firstInitial = GetFirstInitial(givenAscii); // single uppercase char
        bool asian = organisationCode is not null && AsianNocs.Contains(organisationCode);

        // Asian: {Family} {I}.  Western: {I}. {Family}
        return asian
            ? FitTvInitialAsian(family, firstInitial, MaxTvInitialName)
            : FitTvInitialWestern(family, firstInitial, MaxTvInitialName);
    }

    public string BuildTvFamilyName(string familyName)
    {
        var family = NormalizeFamily(familyName);
        return Fit(family, MaxTvFamilyName);
    }

    public string BuildPscbName(string familyName, string? givenName)
    {
        var family = NormalizeFamilyUppercase(familyName);
        if (givenName is null)
            return Fit(family, MaxPscb);

        var given = NormalizeGiven(givenName);
        return FitWithGiven(family, given, MaxPscb);
    }

    public string BuildPscbShortName(string familyName, string? givenName)
    {
        var family = NormalizeFamilyUppercase(familyName);
        if (givenName is null)
            return Fit(family, MaxPscb);

        var givenAscii = NameNormalization.ToAscii(givenName);
        var initials = GetInitials(givenAscii); // uppercase, no dot
        return FitWithInitials(family, initials, MaxPscb, addDotToInitial: false);
    }

    public string BuildPscbLongName(string familyName, string? givenName) =>
        BuildPscbName(familyName, givenName);

    // ── Normalization helpers ────────────────────────────────────────────────

    /// <summary>Limited mixed case for family names (Mc prefix, particles, uppercase rest).</summary>
    private static string NormalizeFamily(string name) =>
        NameNormalization.ToLimitedMixedCase(NameNormalization.ToAscii(name));

    /// <summary>Full uppercase for PSCB family names.</summary>
    private static string NormalizeFamilyUppercase(string name) =>
        NameNormalization.ToAscii(name).ToUpperInvariant();

    /// <summary>Mixed case for given names (title case, Mc, particles).</summary>
    private static string NormalizeGiven(string name) =>
        NameNormalization.ToMixedCase(NameNormalization.ToAscii(name));

    // ── Initials helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns uppercase initials (no dots) from each space- or hyphen-separated part.
    /// "Anne-Marie" → "AM", "John" → "J", "Jose Luis" → "JL".
    /// </summary>
    private static string GetInitials(string name)
    {
        var parts = name.Split([' ', '-'], StringSplitOptions.RemoveEmptyEntries);
        var sb = new System.Text.StringBuilder(parts.Length);
        foreach (var part in parts)
            sb.Append(char.ToUpperInvariant(part[0]));
        return sb.ToString();
    }

    /// <summary>Returns the single uppercase first initial of the name.</summary>
    private static char GetFirstInitial(string name)
    {
        foreach (var ch in name)
        {
            if (char.IsLetter(ch))
                return char.ToUpperInvariant(ch);
        }
        return char.ToUpperInvariant(name[0]);
    }

    // ── Truncation / fitting helpers ─────────────────────────────────────────

    /// <summary>Truncate a family-name-only result.</summary>
    private static string Fit(string family, int max) =>
        NameNormalization.Truncate(family, max);

    /// <summary>
    /// Fit "{family} {given}" within max using the 3-step truncation priority.
    /// Step 1: abbreviate given to first initial + dot.
    /// Step 2: remove given entirely.
    /// Step 3: truncate family.
    /// </summary>
    private static string FitWithGiven(string family, string given, int max)
    {
        var full = $"{family} {given}";
        if (full.Length <= max)
            return full;

        // Step 1: abbreviate given to "X."
        var abbreviated = $"{family} {char.ToUpperInvariant(given[0])}.";
        if (abbreviated.Length <= max)
            return abbreviated;

        // Step 2: remove given
        if (family.Length <= max)
            return family;

        // Step 3: truncate family
        return NameNormalization.Truncate(family, max);
    }

    /// <summary>
    /// Fit "{given} {family}" (western TV order) within max using 3-step truncation.
    /// </summary>
    private static string FitWithGivenWestern(string family, string given, int max)
    {
        var full = $"{given} {family}";
        if (full.Length <= max)
            return full;

        // Step 1: abbreviate given to "X."
        var abbreviated = $"{char.ToUpperInvariant(given[0])}. {family}";
        if (abbreviated.Length <= max)
            return abbreviated;

        // Step 2: remove given
        if (family.Length <= max)
            return family;

        // Step 3: truncate family
        return NameNormalization.Truncate(family, max);
    }

    /// <summary>
    /// Fit "{family} {given}" (Asian TV order) within max using 3-step truncation.
    /// </summary>
    private static string FitAsian(string family, string given, int max)
    {
        var full = $"{family} {given}";
        if (full.Length <= max)
            return full;

        // Step 1: abbreviate given to "X."
        var abbreviated = $"{family} {char.ToUpperInvariant(given[0])}.";
        if (abbreviated.Length <= max)
            return abbreviated;

        // Step 2: remove given
        if (family.Length <= max)
            return family;

        // Step 3: truncate family
        return NameNormalization.Truncate(family, max);
    }

    /// <summary>
    /// Fit "{family} {initials}" or "{initials} {family}" within max for initial-only formats.
    /// Initials are already condensed, so step 1 (abbreviate to initial+dot) applies only
    /// when addDotToInitial=true and initials.Length > 1.
    /// Step 2: remove initials.
    /// Step 3: truncate family.
    /// </summary>
    private static string FitWithInitials(string family, string initials, int max, bool addDotToInitial)
    {
        var full = $"{family} {initials}";
        if (full.Length <= max)
            return full;

        // Step 1 (only if multiple initials and dot mode is enabled)
        if (addDotToInitial && initials.Length > 1)
        {
            var abbreviated = $"{family} {initials[0]}.";
            if (abbreviated.Length <= max)
                return abbreviated;
        }

        // Step 2: remove initials
        if (family.Length <= max)
            return family;

        // Step 3: truncate family
        return NameNormalization.Truncate(family, max);
    }

    /// <summary>Fit "{I}. {family}" (western TV initial) within max.</summary>
    private static string FitTvInitialWestern(string family, char initial, int max)
    {
        var full = $"{initial}. {family}";
        if (full.Length <= max)
            return full;

        // Step 2: remove initial
        if (family.Length <= max)
            return family;

        // Step 3: truncate family
        return NameNormalization.Truncate(family, max);
    }

    /// <summary>Fit "{family} {I}." (Asian TV initial) within max.</summary>
    private static string FitTvInitialAsian(string family, char initial, int max)
    {
        var full = $"{family} {initial}.";
        if (full.Length <= max)
            return full;

        // Step 2: remove initial
        if (family.Length <= max)
            return family;

        // Step 3: truncate family
        return NameNormalization.Truncate(family, max);
    }
}
