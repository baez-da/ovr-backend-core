namespace OVR.Modules.ParticipantRegistry.Domain.NameSystem;

public interface INameBuilder
{
    string BuildPrintName(string familyName, string? givenName);
    string BuildPrintInitialName(string familyName, string? givenName);
    string BuildTvName(string familyName, string? givenName, string? organisationCode = null);
    string BuildTvInitialName(string familyName, string? givenName, string? organisationCode = null);
    string BuildTvFamilyName(string familyName);
    string BuildPscbName(string familyName, string? givenName);
    string BuildPscbShortName(string familyName, string? givenName);
    string BuildPscbLongName(string familyName, string? givenName);
}
