namespace OVR.Modules.CommonCodes.Contracts;

/// <summary>
/// Compile-time constants for CommonCode types used in cross-module business logic (validation,
/// enrichment). These match the Excel sheet names (uppercase) from ODF standard imports.
/// This is NOT an exhaustive list of all types in the database — the DB may contain additional
/// types imported from Excel sheets. Use GET /api/common-codes/types to discover all available types.
/// Add constants here only when a module needs to reference a type in compiled code.
/// </summary>
public static class CommonCodeTypes
{
    public const string Organisation = "ORGANISATIONS";
    public const string Discipline = "DISCIPLINE";
    public const string DisciplineFunction = "DISCIPLINE_FUNCTION";
    public const string FunctionCategory = "FUNCTION_CATEGORY";
    public const string Country = "COUNTRY";
    public const string PersonGender = "PERSON_GENDER";
    public const string Sport = "SPORT";
}
