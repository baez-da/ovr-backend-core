using ErrorOr;

namespace OVR.Modules.DataEntry.Errors;

public static class DataEntryErrors
{
    public static Error UnitResultNotFound(string rsc) =>
        Error.NotFound("DataEntry.UnitResultNotFound",
            $"UnitResult '{rsc}' not found.");

    public static Error InvalidCompetitors(string message) =>
        Error.Validation("DataEntry.InvalidCompetitors", message);
}
