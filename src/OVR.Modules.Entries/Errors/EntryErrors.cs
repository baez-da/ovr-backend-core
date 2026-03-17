using ErrorOr;

namespace OVR.Modules.Entries.Errors;

public static class EntryErrors
{
    public static Error NotFound(string id) =>
        Error.NotFound("Entry.NotFound", $"Entry with ID '{id}' was not found.",
            new Dictionary<string, object> { ["id"] = id });

    public static Error AlreadyExists(string participantId, string eventRsc) =>
        Error.Conflict("Entry.AlreadyExists",
            $"Entry for participant '{participantId}' in event '{eventRsc}' already exists.",
            new Dictionary<string, object> { ["participantId"] = participantId, ["eventRsc"] = eventRsc });

    public static Error InvalidStatusTransition(string from, string to) =>
        Error.Validation("Entry.InvalidStatusTransition",
            $"Cannot transition entry status from '{from}' to '{to}'.",
            new Dictionary<string, object> { ["from"] = from, ["to"] = to });
}
