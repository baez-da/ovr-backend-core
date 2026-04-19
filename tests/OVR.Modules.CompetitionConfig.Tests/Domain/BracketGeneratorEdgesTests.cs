using FluentAssertions;
using OVR.Modules.CompetitionConfig.Domain;
using OVR.SharedKernel.Domain.Progression;
using Xunit;

namespace OVR.Modules.CompetitionConfig.Tests.Domain;

public class BracketGeneratorEdgesTests
{
    private readonly BracketGenerator _sut = new();

    [Fact]
    public void Generate_ForSizeOf8_Produces6Edges()
    {
        var plan = _sut.Generate(CompetitionFormat.SingleElimination, size: 8, startUnitNumber: 1);

        plan.Edges.Should().HaveCount(6);
        plan.Edges.Should().OnlyContain(e => e.Outcome == Outcome.W);
        plan.Edges.Should().OnlyContain(e => e.TargetSlot == 1 || e.TargetSlot == 2);
    }

    [Fact]
    public void Generate_OddSourceUnitNumbers_MapToSlot1()
    {
        var plan = _sut.Generate(CompetitionFormat.SingleElimination, size: 8, startUnitNumber: 1);

        plan.Edges.Should().ContainSingle(e =>
            e.SourceUnitRsc.StartsWith("QFNL0001") && e.TargetSlot == 1);
        plan.Edges.Should().ContainSingle(e =>
            e.SourceUnitRsc.StartsWith("QFNL0003") && e.TargetSlot == 1);
    }

    [Fact]
    public void Generate_EvenSourceUnitNumbers_MapToSlot2()
    {
        var plan = _sut.Generate(CompetitionFormat.SingleElimination, size: 8, startUnitNumber: 1);

        plan.Edges.Should().ContainSingle(e =>
            e.SourceUnitRsc.StartsWith("QFNL0002") && e.TargetSlot == 2);
        plan.Edges.Should().ContainSingle(e =>
            e.SourceUnitRsc.StartsWith("QFNL0004") && e.TargetSlot == 2);
    }

    [Fact]
    public void Generate_DoesNotEmitEdgesOutOfFinal()
    {
        var plan = _sut.Generate(CompetitionFormat.SingleElimination, size: 8, startUnitNumber: 1);

        plan.Edges.Should().NotContain(e => e.SourceUnitRsc.StartsWith("FNL-"));
    }

    [Fact]
    public void Generate_ForSizeOf16_Produces14Edges()
    {
        var plan = _sut.Generate(CompetitionFormat.SingleElimination, size: 16, startUnitNumber: 1);
        plan.Edges.Should().HaveCount(14);
    }

    [Fact]
    public void Generate_ChainsPhasesCorrectly_ForSize4()
    {
        var plan = _sut.Generate(CompetitionFormat.SingleElimination, size: 4, startUnitNumber: 1);

        // Size 4 → SFNL 2 units + FNL 1 unit → 2 edges.
        plan.Edges.Should().HaveCount(2);
        plan.Edges.Should().ContainEquivalentOf(new ProgressionEdge(
            "SFNL0001----", Outcome.W, "FNL-0003----", 1));
        plan.Edges.Should().ContainEquivalentOf(new ProgressionEdge(
            "SFNL0002----", Outcome.W, "FNL-0003----", 2));
    }
}
