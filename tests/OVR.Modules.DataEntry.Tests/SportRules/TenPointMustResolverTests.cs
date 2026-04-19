using FluentAssertions;
using OVR.Modules.DataEntry.Domain;
using OVR.Modules.DataEntry.SportRules;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.DataEntry.Tests.SportRules;

public class TenPointMustResolverTests
{
    private readonly TenPointMustResolver _resolver = new();
    private readonly ParticipantId _red = ParticipantId.Create("NOC-ESP-0001");
    private readonly ParticipantId _blue = ParticipantId.Create("NOC-POL-0014");

    private static IReadOnlyList<Period> Periods(
        (int h1, int a1, int h2, int a2, int h3, int a3)[] judgeTotals)
    {
        return new[] { "R1", "R2", "R3" }.Select((code, rIdx) =>
            new Period(code, Enumerable.Range(0, 3).Select(jIdx =>
            {
                var (h1, a1, h2, a2, h3, a3) = judgeTotals[jIdx];
                var (h, a) = rIdx switch
                {
                    0 => (h1, a1),
                    1 => (h2, a2),
                    _ => (h3, a3),
                };
                return new PeriodScorecard((JudgePosition)jIdx, h, a);
            }).ToList<PeriodScorecard>())).ToList<Period>();
    }

    [Fact]
    public void Unanimous_ForRed_ReturnsWp_3_0_WinnerRed()
    {
        var periods = Periods(new[]
        {
            (10,9,10,9,10,9),
            (10,9,10,9,10,9),
            (10,9,10,9,10,9)
        });

        var d = _resolver.Resolve(periods, _red, _blue);

        d.Type.Should().Be(ResultType.Points);
        d.Code.Should().Be(ResultCode.Wp);
        d.DecisionMark.Should().Be("3:0");
        d.WinnerParticipantId.Should().Be(_red);
    }

    [Fact]
    public void Split_TwoRedOneBlue_ReturnsWp_2_1_WinnerRed()
    {
        // J1: 10+10+9=29 home, 9+9+10=28 away → red
        // J2: same → red
        // J3: 9+9+10=28 home, 10+10+9=29 away → blue
        var periods = Periods(new[]
        {
            (10,9,10,9,9,10),
            (10,9,10,9,9,10),
            (9,10,9,10,10,9)
        });

        var d = _resolver.Resolve(periods, _red, _blue);

        d.Type.Should().Be(ResultType.Points);
        d.Code.Should().Be(ResultCode.Wp);
        d.DecisionMark.Should().Be("2:1");
        d.WinnerParticipantId.Should().Be(_red);
    }

    [Fact]
    public void Majority_TwoRedOneDraw_ReturnsWp_2_0_WinnerRed()
    {
        // J1, J2: red wins; J3: draw (totals 29/29)
        var periods = Periods(new[]
        {
            (10,9,10,9,9,10),
            (10,9,10,9,9,10),
            (10,10,9,9,10,10)
        });

        var d = _resolver.Resolve(periods, _red, _blue);

        d.Type.Should().Be(ResultType.Points);
        d.Code.Should().Be(ResultCode.Wp);
        d.DecisionMark.Should().Be("2:0");
        d.WinnerParticipantId.Should().Be(_red);
    }

    [Fact]
    public void AllDraws_ReturnsNc()
    {
        var periods = Periods(new[]
        {
            (10,10,9,9,10,10),
            (10,10,9,9,10,10),
            (10,10,9,9,10,10)
        });

        var d = _resolver.Resolve(periods, _red, _blue);

        d.Type.Should().Be(ResultType.Rm);
        d.Code.Should().Be(ResultCode.Nc);
        d.WinnerParticipantId.Should().BeNull();
        d.DecisionMark.Should().BeNull();
    }

    [Fact]
    public void SplitWithOneDraw_OneRedOneBlueOneDraw_ReturnsNc()
    {
        var periods = Periods(new[]
        {
            (10,9,10,9,10,9),     // J1 → 30-27 red
            (9,10,9,10,9,10),     // J2 → 27-30 blue
            (10,10,9,9,10,10)     // J3 → 29-29 draw
        });

        var d = _resolver.Resolve(periods, _red, _blue);

        d.Type.Should().Be(ResultType.Rm);
        d.Code.Should().Be(ResultCode.Nc);
        d.WinnerParticipantId.Should().BeNull();
    }
}
