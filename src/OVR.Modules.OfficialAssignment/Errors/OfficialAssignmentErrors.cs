using ErrorOr;

namespace OVR.Modules.OfficialAssignment.Errors;

public static class OfficialAssignmentErrors
{
    public static Error NotFound(string id) =>
        Error.NotFound("OfficialAssignment.NotFound", $"Official assignment with ID '{id}' was not found.");

    public static Error AlreadyExists(string participantId, string unitRsc) =>
        Error.Conflict("OfficialAssignment.AlreadyExists", $"Official assignment for participant '{participantId}' in unit '{unitRsc}' already exists.");

    public static Error InvalidStatusTransition(string from, string to) =>
        Error.Validation("OfficialAssignment.InvalidStatusTransition", $"Cannot transition assignment status from '{from}' to '{to}'.");
}
