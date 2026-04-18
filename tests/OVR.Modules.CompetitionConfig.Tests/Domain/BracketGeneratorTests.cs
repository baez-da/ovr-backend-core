using FluentAssertions;
using OVR.Modules.CompetitionConfig.Domain;

namespace OVR.Modules.CompetitionConfig.Tests.Domain;

public class BracketGeneratorTests
{
    private readonly BracketGenerator _generator = new();

    [Fact]
    public void Generate_WithSize2_ReturnsSinglePhaseWithOneUnit()
    {
        var plan = _generator.Generate(CompetitionFormat.SingleElimination, size: 2, startUnitNumber: 1);

        plan.Phases.Should().HaveCount(1);
        plan.Phases[0].Code.Should().Be(PhaseCodes.Final);
        plan.Phases[0].Order.Should().Be(0);
        plan.Phases[0].UnitCount.Should().Be(1);
        plan.UnitLocalSegments.Should().HaveCount(1);
        plan.UnitLocalSegments[0].Should().Be("FNL-0001----");
    }

    [Fact]
    public void Generate_WithSize4_Returns_SFNL_FNL_WithCorrectUnitCounts()
    {
        var plan = _generator.Generate(CompetitionFormat.SingleElimination, size: 4, startUnitNumber: 1);

        plan.Phases.Select(p => p.Code).Should().Equal(PhaseCodes.SemiFinals, PhaseCodes.Final);
        plan.Phases.Select(p => p.UnitCount).Should().Equal(2, 1);
        plan.UnitLocalSegments.Should().HaveCount(3);
        plan.UnitLocalSegments.Should().ContainInOrder(
            "SFNL0001----", "SFNL0002----", "FNL-0003----");
    }

    [Fact]
    public void Generate_WithSize8_Returns_QFNL_SFNL_FNL()
    {
        var plan = _generator.Generate(CompetitionFormat.SingleElimination, size: 8, startUnitNumber: 1);

        plan.Phases.Select(p => p.Code).Should().Equal(
            PhaseCodes.QuarterFinals, PhaseCodes.SemiFinals, PhaseCodes.Final);
        plan.Phases.Select(p => p.UnitCount).Should().Equal(4, 2, 1);
        plan.UnitLocalSegments.Should().HaveCount(7);
    }

    [Fact]
    public void Generate_WithSize16_Returns_8FNL_QFNL_SFNL_FNL()
    {
        var plan = _generator.Generate(CompetitionFormat.SingleElimination, size: 16, startUnitNumber: 1);

        plan.Phases.Select(p => p.Code).Should().Equal(
            PhaseCodes.EighthFinals, PhaseCodes.QuarterFinals,
            PhaseCodes.SemiFinals, PhaseCodes.Final);
        plan.Phases.Select(p => p.UnitCount).Should().Equal(8, 4, 2, 1);
        plan.UnitLocalSegments.Should().HaveCount(15);
        plan.UnitLocalSegments[0].Should().Be("8FNL0001----");
        plan.UnitLocalSegments[^1].Should().Be("FNL-0015----");
    }

    [Fact]
    public void Generate_WithSize32_Returns_R32_through_FNL()
    {
        var plan = _generator.Generate(CompetitionFormat.SingleElimination, size: 32, startUnitNumber: 1);

        plan.Phases.Select(p => p.Code).Should().Equal(
            PhaseCodes.R32, PhaseCodes.EighthFinals, PhaseCodes.QuarterFinals,
            PhaseCodes.SemiFinals, PhaseCodes.Final);
        plan.Phases.Select(p => p.UnitCount).Should().Equal(16, 8, 4, 2, 1);
        plan.UnitLocalSegments.Should().HaveCount(31);
    }

    [Fact]
    public void Generate_WithSize13_RoundsUpToM16_WithSamePhases()
    {
        var plan = _generator.Generate(CompetitionFormat.SingleElimination, size: 13, startUnitNumber: 1);

        plan.Phases.Select(p => p.Code).Should().Equal(
            PhaseCodes.EighthFinals, PhaseCodes.QuarterFinals,
            PhaseCodes.SemiFinals, PhaseCodes.Final);
        plan.UnitLocalSegments.Should().HaveCount(15);
    }

    [Fact]
    public void Generate_WithSize33_RoundsUpToM64()
    {
        var plan = _generator.Generate(CompetitionFormat.SingleElimination, size: 33, startUnitNumber: 1);

        plan.Phases[0].Code.Should().Be(PhaseCodes.R64);
        plan.UnitLocalSegments.Should().HaveCount(63);
    }

    [Fact]
    public void Generate_StartingAtUnitNumber5_FirstUnitSegmentStartsWith_0005()
    {
        var plan = _generator.Generate(CompetitionFormat.SingleElimination, size: 4, startUnitNumber: 5);

        plan.UnitLocalSegments[0].Should().Be("SFNL0005----");
        plan.UnitLocalSegments[^1].Should().Be("FNL-0007----");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(129)]
    [InlineData(500)]
    public void Generate_WithOutOfRangeSize_Throws(int size)
    {
        Action act = () => _generator.Generate(CompetitionFormat.SingleElimination, size, startUnitNumber: 1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Generate_WithUnsupportedFormat_Throws()
    {
        Action act = () => _generator.Generate((CompetitionFormat)999, size: 16, startUnitNumber: 1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
