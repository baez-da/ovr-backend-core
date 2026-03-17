using ErrorOr;

namespace OVR.Modules.Entries.Errors;

public static class EntryErrors
{
    public static Error NotFound(string id) =>
        Error.NotFound("Entry.NotFound", $"Entry with ID '{id}' was not found.");

    public static Error AlreadyExists(string participantId, string eventRsc) =>
        Error.Conflict("Entry.AlreadyExists", $"Entry for participant '{participantId}' in event '{eventRsc}' already exists.");

    public static Error InvalidStatusTransition(string from, string to) =>
        Error.Validation("Entry.InvalidStatusTransition", $"Cannot transition entry status from '{from}' to '{to}'.");
}
