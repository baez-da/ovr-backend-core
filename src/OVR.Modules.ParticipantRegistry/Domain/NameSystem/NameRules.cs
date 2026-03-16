namespace OVR.Modules.ParticipantRegistry.Domain.NameSystem;

public static class NameRules
{
    public static string BuildPrintName(string familyName, string givenName) =>
        $"{familyName.ToUpperInvariant()} {givenName}";

    public static string BuildTvName(string familyName, string givenName) =>
        $"{familyName.ToUpperInvariant()} {givenName[0]}.";

    public static string BuildPscbName(string familyName, string givenName) =>
        $"{familyName.ToUpperInvariant()} {givenName}";
}
