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

    public static Error InvalidFunction(string functionId) =>
        Error.Validation("Participant.InvalidFunction",
            $"Function '{functionId}' does not exist in CommonCodes.",
            new Dictionary<string, object> { ["functionId"] = functionId });

    public static Error InvalidDiscipline(string disciplineCode) =>
        Error.Validation("Participant.InvalidDiscipline",
            $"Discipline '{disciplineCode}' does not exist in CommonCodes.",
            new Dictionary<string, object> { ["disciplineCode"] = disciplineCode });
}
