namespace OVR.Modules.DataEntry.SportRules;

public static class BoxingRules
{
    public const int PeriodCount = 3;
    public const int JudgeCount = 3;
    public const int MinPeriodScore = 6;
    public const int MaxPeriodScore = 10;

    public static readonly IReadOnlyList<string> PeriodCodes = new[] { "R1", "R2", "R3" };
}
