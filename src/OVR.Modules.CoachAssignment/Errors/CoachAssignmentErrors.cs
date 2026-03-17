using ErrorOr;

namespace OVR.Modules.CoachAssignment.Errors;

public static class CoachAssignmentErrors
{
    public static Error NotFound(string id) =>
        Error.NotFound("CoachAssignment.NotFound", $"Coach assignment with ID '{id}' was not found.");

    public static Error AlreadyExists(string participantId, string eventRsc) =>
        Error.Conflict("CoachAssignment.AlreadyExists", $"Coach assignment for participant '{participantId}' in event '{eventRsc}' already exists.");

    public static Error InvalidStatusTransition(string from, string to) =>
        Error.Validation("CoachAssignment.InvalidStatusTransition", $"Cannot transition coach assignment status from '{from}' to '{to}'.");
}
