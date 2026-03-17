using ErrorOr;

namespace OVR.Modules.CoachAssignment.Errors;

public static class CoachAssignmentErrors
{
    public static Error NotFound(string id) =>
        Error.NotFound("CoachAssignment.NotFound", $"Coach assignment with ID '{id}' was not found.",
            new Dictionary<string, object> { ["id"] = id });

    public static Error AlreadyExists(string participantId, string eventRsc) =>
        Error.Conflict("CoachAssignment.AlreadyExists",
            $"Coach assignment for participant '{participantId}' in event '{eventRsc}' already exists.",
            new Dictionary<string, object> { ["participantId"] = participantId, ["eventRsc"] = eventRsc });

    public static Error InvalidStatusTransition(string from, string to) =>
        Error.Validation("CoachAssignment.InvalidStatusTransition",
            $"Cannot transition coach assignment status from '{from}' to '{to}'.",
            new Dictionary<string, object> { ["from"] = from, ["to"] = to });
}
