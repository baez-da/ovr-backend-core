using ErrorOr;

namespace OVR.Ingestion.Errors;

public static class IngestionErrors
{
    public static Error InvalidDocumentType(string documentType) =>
        Error.Validation("Ingestion.InvalidDocumentType",
            $"Expected DT_PARTIC but received '{documentType}'.",
            new Dictionary<string, object> { ["documentType"] = documentType });

    public static Error EmptyParticipantList() =>
        Error.Validation("Ingestion.EmptyParticipantList",
            "The XML contains no participants.");

    public static Error MalformedXml(string reason) =>
        Error.Validation("Ingestion.MalformedXml",
            $"The XML file could not be parsed: {reason}.",
            new Dictionary<string, object> { ["reason"] = reason });
}
