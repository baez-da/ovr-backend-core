using FluentAssertions;
using OVR.Modules.DataEntry.Domain;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.DataEntry.Tests.Domain;

public class UnitResultAggregateTests
{
    protected static Rsc MakeUnitRsc() =>
        Rsc.Create("BOXM57KG--------------8FNL0001----");

    protected static Competitor Red() =>
        new(1, ParticipantId.Create("NOC-ESP-0001"), null, 1,
            Organisation.Create("ESP"), null);

    protected static Competitor Blue() =>
        new(2, ParticipantId.Create("NOC-POL-0014"), null, 8,
            Organisation.Create("POL"), null);

    [Fact]
    public void CreateForFirstRound_WithValidCompetitors_SucceedsInStartList()
    {
        var rsc = MakeUnitRsc();
        var result = UnitResult.CreateForFirstRound(rsc, Red(), Blue());

        result.IsError.Should().BeFalse();
        var ur = result.Value;
        ur.Status.Should().Be(ResultStatus.StartList);
        ur.Competitors.Should().HaveCount(2);
        ur.Competitors[0].SortOrder.Should().Be(1);
        ur.Competitors[1].SortOrder.Should().Be(2);
    }

    [Fact]
    public void CreateForFirstRound_WithDuplicateSortOrder_ReturnsError()
    {
        var rsc = MakeUnitRsc();
        var red = Red();
        var redAgain = red with { SortOrder = 1 };

        var result = UnitResult.CreateForFirstRound(rsc, red, redAgain);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("DataEntry.InvalidCompetitors");
    }

    [Fact]
    public void CreateForFirstRound_WithWrongSortOrderValues_ReturnsError()
    {
        var rsc = MakeUnitRsc();
        var a = Red() with { SortOrder = 3 };
        var b = Blue() with { SortOrder = 4 };

        var result = UnitResult.CreateForFirstRound(rsc, a, b);

        result.IsError.Should().BeTrue();
    }

    // ── Task 11: Start ────────────────────────────────────────────────────────

    private static UnitResult NewInStartList()
    {
        var rsc = MakeUnitRsc();
        return UnitResult.CreateForFirstRound(rsc, Red(), Blue()).Value;
    }

    [Fact]
    public void Start_FromStartList_TransitionsToLive()
    {
        var ur = NewInStartList();
        var result = ur.Start();

        result.IsError.Should().BeFalse();
        ur.Status.Should().Be(ResultStatus.Live);
        ur.StartedAt.Should().NotBeNull();
        ur.CurrentPeriodCode.Should().Be("R1");
    }

    [Fact]
    public void Start_WhenAlreadyLive_ReturnsError()
    {
        var ur = NewInStartList();
        ur.Start();

        var again = ur.Start();
        again.IsError.Should().BeTrue();
        again.FirstError.Code.Should().Be("DataEntry.InvalidStatusTransition");
    }

    // ── Task 12: ScorePeriod ──────────────────────────────────────────────────

    private static IReadOnlyList<PeriodScorecard> EvenCards(int home, int away) => new[]
    {
        new PeriodScorecard(JudgePosition.J1, home, away),
        new PeriodScorecard(JudgePosition.J2, home, away),
        new PeriodScorecard(JudgePosition.J3, home, away)
    };

    [Fact]
    public void ScorePeriod_FromStartList_Fails()
    {
        var ur = NewInStartList();
        var result = ur.ScorePeriod("R1", EvenCards(10, 9));
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("DataEntry.InvalidStatusTransition");
    }

    [Fact]
    public void ScorePeriod_R1_InLive_Succeeds_AndAdvancesCurrentPeriodToR2()
    {
        var ur = NewInStartList();
        ur.Start();

        var result = ur.ScorePeriod("R1", EvenCards(10, 9));

        result.IsError.Should().BeFalse();
        ur.Periods.Should().HaveCount(1);
        ur.Periods[0].Code.Should().Be("R1");
        ur.CurrentPeriodCode.Should().Be("R2");
    }

    [Fact]
    public void ScorePeriod_R2BeforeR1_ReturnsInvalidPeriodOrder()
    {
        var ur = NewInStartList();
        ur.Start();

        var result = ur.ScorePeriod("R2", EvenCards(10, 9));

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("DataEntry.InvalidPeriodOrder");
    }

    [Fact]
    public void ScorePeriod_SameR1Twice_ReturnsPeriodAlreadyScored()
    {
        var ur = NewInStartList();
        ur.Start();
        ur.ScorePeriod("R1", EvenCards(10, 9));

        var result = ur.ScorePeriod("R1", EvenCards(10, 9));
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("DataEntry.PeriodAlreadyScored");
    }

    [Fact]
    public void ScorePeriod_With4Scorecards_ReturnsInvalidScorecardCount()
    {
        var ur = NewInStartList();
        ur.Start();

        var fourCards = EvenCards(10, 9).Append(
            new PeriodScorecard(JudgePosition.J1, 10, 9)).ToList();

        var result = ur.ScorePeriod("R1", fourCards);
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("DataEntry.InvalidScorecardCount");
    }

    [Fact]
    public void ScorePeriod_WithScore5_ReturnsInvalidScoreRange()
    {
        var ur = NewInStartList();
        ur.Start();

        var cards = new[]
        {
            new PeriodScorecard(JudgePosition.J1, 10, 5),
            new PeriodScorecard(JudgePosition.J2, 10, 9),
            new PeriodScorecard(JudgePosition.J3, 10, 9)
        };

        var result = ur.ScorePeriod("R1", cards);
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("DataEntry.InvalidScoreRange");
    }

    [Fact]
    public void ScorePeriod_WithDuplicateJudge_ReturnsDuplicateJudgePosition()
    {
        var ur = NewInStartList();
        ur.Start();

        var cards = new[]
        {
            new PeriodScorecard(JudgePosition.J1, 10, 9),
            new PeriodScorecard(JudgePosition.J1, 10, 9),
            new PeriodScorecard(JudgePosition.J2, 10, 9)
        };

        var result = ur.ScorePeriod("R1", cards);
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("DataEntry.DuplicateJudgePosition");
    }

    [Fact]
    public void ScorePeriod_InvalidCode_ReturnsInvalidPeriodCode()
    {
        var ur = NewInStartList();
        ur.Start();

        var result = ur.ScorePeriod("R4", EvenCards(10, 9));
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("DataEntry.InvalidPeriodCode");
    }
}
