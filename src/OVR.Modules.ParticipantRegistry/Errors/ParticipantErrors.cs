using ErrorOr;

namespace OVR.Modules.ParticipantRegistry.Errors;

public static class ParticipantErrors
{
    public static Error NotFound(string id) =>
        Error.NotFound("Participant.NotFound", $"Participant with ID '{id}' was not found.",
            new Dictionary<string, object> { ["id"] = id });

    public static Error InvalidOrganisation(string code) =>
        Error.Validation("Participant.InvalidOrganisation", $"Organisation '{code}' does not exist in CommonCodes.",
            new Dictionary<string, object> { ["code"] = code });

    public static Error InvalidFunction(string function) =>
        Error.Validation("Participant.InvalidFunction",
            $"Function '{function}' does not exist in CommonCodes.",
            new Dictionary<string, object> { ["function"] = function });

    public static Error InvalidDiscipline(string discipline) =>
        Error.Validation("Participant.InvalidDiscipline",
            $"Discipline '{discipline}' does not exist in CommonCodes.",
            new Dictionary<string, object> { ["discipline"] = discipline });
}
