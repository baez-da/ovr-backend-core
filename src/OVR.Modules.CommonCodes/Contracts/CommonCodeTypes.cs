namespace OVR.Modules.CommonCodes.Contracts;

/// <summary>
/// Constants for CommonCode type names. These match the Excel sheet names (uppercase)
/// used during import and stored as the Type field in MongoDB.
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
