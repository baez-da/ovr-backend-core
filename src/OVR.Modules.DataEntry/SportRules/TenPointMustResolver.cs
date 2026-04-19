using OVR.Modules.DataEntry.Domain;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.DataEntry.SportRules;

public interface ITenPointMustResolver
{
    Decision Resolve(
        IReadOnlyList<Period> periods,
        ParticipantId redParticipant,
        ParticipantId blueParticipant);
}

public sealed class TenPointMustResolver : ITenPointMustResolver
{
    public Decision Resolve(
        IReadOnlyList<Period> periods,
        ParticipantId redParticipant,
        ParticipantId blueParticipant)
    {
        // Aggregate per-judge totals across all periods.
        var judgeTotals = new Dictionary<JudgePosition, (int Home, int Away)>();
        foreach (var period in periods)
        {
            foreach (var card in period.Scorecards)
            {
                var current = judgeTotals.TryGetValue(card.JudgePos, out var v)
                    ? v : (Home: 0, Away: 0);
                judgeTotals[card.JudgePos] =
                    (current.Home + card.HomeScore, current.Away + card.AwayScore);
            }
        }

        int redVotes = 0, blueVotes = 0, drawVotes = 0;
        foreach (var (H, A) in judgeTotals.Values)
        {
            if (H > A) redVotes++;
            else if (A > H) blueVotes++;
            else drawVotes++;
        }

        if (redVotes >= 2 && blueVotes == 0)
        {
            var mark = drawVotes == 0 ? $"{redVotes}:{blueVotes}" : $"{redVotes}:0";
            return new Decision(
                ResultType.Points, ResultCode.Wp, mark,
                StoppageRound: null, StoppageTime: null,
                WinnerParticipantId: redParticipant);
        }
        if (blueVotes >= 2 && redVotes == 0)
        {
            var mark = drawVotes == 0 ? $"{blueVotes}:{redVotes}" : $"{blueVotes}:0";
            return new Decision(
                ResultType.Points, ResultCode.Wp, mark,
                StoppageRound: null, StoppageTime: null,
                WinnerParticipantId: blueParticipant);
        }
        if (redVotes == 2 && blueVotes == 1)
            return new Decision(ResultType.Points, ResultCode.Wp, "2:1",
                null, null, redParticipant);
        if (blueVotes == 2 && redVotes == 1)
            return new Decision(ResultType.Points, ResultCode.Wp, "2:1",
                null, null, blueParticipant);

        return new Decision(ResultType.Rm, ResultCode.Nc,
            DecisionMark: null, StoppageRound: null, StoppageTime: null,
            WinnerParticipantId: null);
    }
}
