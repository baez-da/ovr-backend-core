using ErrorOr;

namespace OVR.Modules.CommonCodes.Errors;

public static class CommonCodeErrors
{
    public static Error InvalidFile(string reason) =>
        Error.Validation("CommonCodes.InvalidFile", reason);

    public static Error ImportFailed(string reason) =>
        Error.Failure("CommonCodes.ImportFailed", reason);

    public static Error TypeNotFound(string type) =>
        Error.NotFound("CommonCodes.TypeNotFound", $"No common codes found for type '{type}'.");
}
