using ErrorOr;

namespace OVR.Modules.Progression.Errors;

public static class ProgressionErrors
{
    public static Error BracketNotFound(string eventRsc) =>
        Error.NotFound(
            code: "Progression.BracketNotFound",
            description: "Bracket progression not found.",
            metadata: new Dictionary<string, object> { ["eventRsc"] = eventRsc });

    public static Error DuplicateEdge(string sourceUnitRsc, string outcome) =>
        Error.Validation(
            code: "Progression.DuplicateEdge",
            description: "Duplicate progression edge.",
            metadata: new Dictionary<string, object>
            {
                ["sourceUnitRsc"] = sourceUnitRsc,
                ["outcome"] = outcome
            });

    public static Error DuplicateTargetSlot(string targetUnitRsc, int targetSlot) =>
        Error.Validation(
            code: "Progression.DuplicateTargetSlot",
            description: "Target slot already fed by another edge.",
            metadata: new Dictionary<string, object>
            {
                ["targetUnitRsc"] = targetUnitRsc,
                ["targetSlot"] = targetSlot
            });

    public static Error InvalidSlot(int slot) =>
        Error.Validation(
            code: "Progression.InvalidSlot",
            description: "Slot must be 1 or 2.",
            metadata: new Dictionary<string, object> { ["slot"] = slot });

    public static Error InvalidEventRsc() =>
        Error.Validation(
            code: "Progression.InvalidEventRsc",
            description: "EventRsc is required.");
}
