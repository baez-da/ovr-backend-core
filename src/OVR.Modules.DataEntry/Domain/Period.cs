namespace OVR.Modules.DataEntry.Domain;

public sealed record Period(
    string Code,
    IReadOnlyList<PeriodScorecard> Scorecards);
