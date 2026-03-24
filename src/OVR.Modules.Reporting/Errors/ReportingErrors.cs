using ErrorOr;

namespace OVR.Modules.Reporting.Errors;

public static class ReportingErrors
{
    public static Error TemplateNotFound(string orisCode, string? discipline) =>
        Error.NotFound("Reporting.TemplateNotFound",
            $"No template found for '{orisCode}' (discipline: {discipline ?? "generic"}).",
            new Dictionary<string, object>
            {
                ["orisCode"] = orisCode,
                ["discipline"] = discipline ?? "generic"
            });

    public static Error DataProviderNotFound(string orisCode, string? discipline) =>
        Error.NotFound("Reporting.DataProviderNotFound",
            $"No data provider found for '{orisCode}' (discipline: {discipline ?? "generic"}).",
            new Dictionary<string, object>
            {
                ["orisCode"] = orisCode,
                ["discipline"] = discipline ?? "generic"
            });

    public static Error LayoutNotFound(string component) =>
        Error.NotFound("Reporting.LayoutNotFound",
            $"No layout component '{component}' found.",
            new Dictionary<string, object> { ["component"] = component });

    public static Error ReportNotFound(string rsc, string orisCode) =>
        Error.NotFound("Reporting.ReportNotFound",
            $"No published report found for RSC '{rsc}' with ORIS code '{orisCode}'.",
            new Dictionary<string, object> { ["rsc"] = rsc, ["orisCode"] = orisCode });

    public static Error S3UploadFailed(string reason) =>
        Error.Failure("Reporting.S3UploadFailed",
            $"Failed to upload report to storage: {reason}.",
            new Dictionary<string, object> { ["reason"] = reason });

    public static Error PdfGenerationFailed(string reason) =>
        Error.Failure("Reporting.PdfGenerationFailed",
            $"PDF generation failed: {reason}.",
            new Dictionary<string, object> { ["reason"] = reason });
}
