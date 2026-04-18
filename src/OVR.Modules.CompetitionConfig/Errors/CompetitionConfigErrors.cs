using ErrorOr;

namespace OVR.Modules.CompetitionConfig.Errors;

public static class CompetitionConfigErrors
{
    public static Error InvalidDiscipline(string code) =>
        Error.Validation(
            "CompetitionConfig.InvalidDiscipline",
            "Discipline code is not in the common codes catalog.",
            new Dictionary<string, object> { ["discipline"] = code });

    public static Error InvalidEventCode(string code) =>
        Error.Validation(
            "CompetitionConfig.InvalidEventCode",
            "Event code is not in the common codes catalog.",
            new Dictionary<string, object> { ["eventCode"] = code });

    public static Error EventAlreadyExists(string rsc) =>
        Error.Conflict(
            "CompetitionConfig.EventAlreadyExists",
            "An event with this RSC already exists.",
            new Dictionary<string, object> { ["rsc"] = rsc });

    public static Error EventNotFound(string rsc) =>
        Error.NotFound(
            "CompetitionConfig.EventNotFound",
            "Event not found.",
            new Dictionary<string, object> { ["rsc"] = rsc });

    public static Error StructureAlreadyGenerated(string rsc) =>
        Error.Conflict(
            "CompetitionConfig.StructureAlreadyGenerated",
            "Structure was already generated for this event.",
            new Dictionary<string, object> { ["rsc"] = rsc });

    public static Error UnsupportedFormat(string format) =>
        Error.Validation(
            "CompetitionConfig.UnsupportedFormat",
            "Competition format not supported in this version.",
            new Dictionary<string, object> { ["format"] = format });

    public static Error InvalidSize(int size) =>
        Error.Validation(
            "CompetitionConfig.InvalidSize",
            "Size must be between 2 and 128.",
            new Dictionary<string, object> { ["size"] = size });
}
