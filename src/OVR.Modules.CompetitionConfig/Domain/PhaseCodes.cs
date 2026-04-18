namespace OVR.Modules.CompetitionConfig.Domain;

public static class PhaseCodes
{
    // Knockouts (used in MVP for single-elimination)
    public const string R128 = "R128";
    public const string R64 = "R64-";
    public const string R32 = "R32-";
    public const string EighthFinals = "8FNL";    // Round of 16
    public const string QuarterFinals = "QFNL";
    public const string SemiFinals = "SFNL";
    public const string Final = "FNL-";

    // Reference constants for future use
    public const string Preliminaries = "PREL";
    public const string Qualification = "QUAL";
    public const string Heat = "HEAT";
    public const string LuckyLoser = "LL--";
    public const string Repechage = "REP-";
}
