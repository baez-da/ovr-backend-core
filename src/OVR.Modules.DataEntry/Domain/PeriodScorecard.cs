namespace OVR.Modules.DataEntry.Domain;

public sealed record PeriodScorecard(
    JudgePosition JudgePos,
    int HomeScore,
    int AwayScore);
