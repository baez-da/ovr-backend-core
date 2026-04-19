using MongoDB.Bson.Serialization.Attributes;

namespace OVR.Modules.DataEntry.Persistence;

public sealed class UnitResultDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
    public List<CompetitorDocument> Competitors { get; set; } = new();
    public List<PeriodDocument> Periods { get; set; } = new();
    public DecisionDocument? Decision { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? CurrentPeriodCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class CompetitorDocument
{
    public int SortOrder { get; set; }
    public string? ParticipantId { get; set; }
    public string? NocompDetail { get; set; }
    public int? Seed { get; set; }
    public string Organisation { get; set; } = string.Empty;
    public string? Wlt { get; set; }
}

public sealed class PeriodDocument
{
    public string Code { get; set; } = string.Empty;
    public List<ScorecardDocument> Scorecards { get; set; } = new();
}

public sealed class ScorecardDocument
{
    public string JudgePos { get; set; } = string.Empty;
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
}

public sealed class DecisionDocument
{
    public string Type { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? DecisionMark { get; set; }
    public string? StoppageRound { get; set; }
    public string? StoppageTime { get; set; }
    public string? WinnerParticipantId { get; set; }
}
