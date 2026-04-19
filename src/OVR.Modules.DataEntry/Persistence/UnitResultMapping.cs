using OVR.Modules.DataEntry.Domain;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.DataEntry.Persistence;

public static class UnitResultMapping
{
    public static UnitResultDocument ToDocument(UnitResult ur) => new()
    {
        Id = ur.UnitRsc.Value,
        Status = ur.Status.ToString(),
        Competitors = ur.Competitors.Select(c => new CompetitorDocument
        {
            SortOrder = c.SortOrder,
            ParticipantId = c.ParticipantId?.Value,
            NocompDetail = c.NocompDetail,
            Seed = c.Seed,
            Organisation = c.Organisation.Code,
            Wlt = c.Wlt?.ToString()
        }).ToList(),
        Periods = ur.Periods.Select(p => new PeriodDocument
        {
            Code = p.Code,
            Scorecards = p.Scorecards.Select(s => new ScorecardDocument
            {
                JudgePos = s.JudgePos.ToString(),
                HomeScore = s.HomeScore,
                AwayScore = s.AwayScore
            }).ToList()
        }).ToList(),
        Decision = ur.Decision is null ? null : new DecisionDocument
        {
            Type = ur.Decision.Type.ToString(),
            Code = ur.Decision.Code.ToString(),
            DecisionMark = ur.Decision.DecisionMark,
            StoppageRound = ur.Decision.StoppageRound,
            StoppageTime = ur.Decision.StoppageTime,
            WinnerParticipantId = ur.Decision.WinnerParticipantId?.Value
        },
        StartedAt = ur.StartedAt,
        EndedAt = ur.EndedAt,
        CurrentPeriodCode = ur.CurrentPeriodCode,
        CreatedAt = ur.CreatedAt,
        UpdatedAt = ur.UpdatedAt
    };

    public static UnitResult ToDomain(UnitResultDocument doc)
    {
        var competitors = doc.Competitors.Select(c => new Competitor(
            SortOrder: c.SortOrder,
            ParticipantId: c.ParticipantId is null ? null : ParticipantId.Create(c.ParticipantId),
            NocompDetail: c.NocompDetail,
            Seed: c.Seed,
            Organisation: Organisation.Create(c.Organisation),
            Wlt: c.Wlt is null ? null : Enum.Parse<Wlt>(c.Wlt))).ToList();

        var periods = doc.Periods.Select(p => new Period(
            p.Code,
            p.Scorecards.Select(s => new PeriodScorecard(
                Enum.Parse<JudgePosition>(s.JudgePos),
                s.HomeScore,
                s.AwayScore)).ToList())).ToList();

        var decision = doc.Decision is null ? null : new Decision(
            Enum.Parse<ResultType>(doc.Decision.Type),
            Enum.Parse<ResultCode>(doc.Decision.Code),
            doc.Decision.DecisionMark,
            doc.Decision.StoppageRound,
            doc.Decision.StoppageTime,
            doc.Decision.WinnerParticipantId is null
                ? null : ParticipantId.Create(doc.Decision.WinnerParticipantId));

        return UnitResult.Hydrate(
            Rsc.Create(doc.Id),
            Enum.Parse<ResultStatus>(doc.Status),
            competitors,
            periods,
            decision,
            doc.StartedAt,
            doc.EndedAt,
            doc.CurrentPeriodCode,
            doc.CreatedAt,
            doc.UpdatedAt);
    }
}
