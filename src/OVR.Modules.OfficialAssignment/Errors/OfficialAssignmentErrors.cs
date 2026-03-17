using ErrorOr;

namespace OVR.Modules.OfficialAssignment.Errors;

public static class OfficialAssignmentErrors
{
    public static Error NotFound(string id) =>
        Error.NotFound("OfficialAssignment.NotFound", $"Official assignment with ID '{id}' was not found.",
            new Dictionary<string, object> { ["id"] = id });

    public static Error AlreadyExists(string participantId, string unitRsc) =>
        Error.Conflict("OfficialAssignment.AlreadyExists",
            $"Official assignment for participant '{participantId}' in unit '{unitRsc}' already exists.",
            new Dictionary<string, object> { ["participantId"] = participantId, ["unitRsc"] = unitRsc });

    public static Error InvalidStatusTransition(string from, string to) =>
        Error.Validation("OfficialAssignment.InvalidStatusTransition",
            $"Cannot transition assignment status from '{from}' to '{to}'.",
            new Dictionary<string, object> { ["from"] = from, ["to"] = to });
}
