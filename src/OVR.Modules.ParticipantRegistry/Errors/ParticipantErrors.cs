using ErrorOr;

namespace OVR.Modules.ParticipantRegistry.Errors;

public static class ParticipantErrors
{
    public static Error NotFound(string id) =>
        Error.NotFound("Participant.NotFound", $"Participant with ID '{id}' was not found.",
            new Dictionary<string, object> { ["id"] = id });

    public static Error AlreadyExists(string id) =>
        Error.Conflict("Participant.AlreadyExists", $"Participant with ID '{id}' already exists.",
            new Dictionary<string, object> { ["id"] = id });
}
