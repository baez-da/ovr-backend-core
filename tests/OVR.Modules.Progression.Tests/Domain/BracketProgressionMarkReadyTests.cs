using FluentAssertions;
using OVR.Modules.Progression.Domain;
using OVR.SharedKernel.Domain.Progression;

namespace OVR.Modules.Progression.Tests.Domain;

public class BracketProgressionMarkReadyTests
{
    private static BracketProgression MakeAggregate() =>
        BracketProgression.Create("EVT123", new[]
        {
            new ProgressionEdge("SFNL0001----", Outcome.W, "FNL-0001----", 1),
            new ProgressionEdge("SFNL0002----", Outcome.W, "FNL-0001----", 2)
        }).Value;

    [Fact]
    public void MarkTargetReady_WithBufferedPending_FlushesAndClears()
    {
        var agg = MakeAggregate();
        agg.RecordAdvancement("SFNL0001----", Outcome.W, "P1");
        agg.RecordAdvancement("SFNL0002----", Outcome.W, "P2");
        agg.PendingAdvancements.Should().HaveCount(2);

        var flushed = agg.MarkTargetReady("FNL-0001----");

        flushed.Should().HaveCount(2);
        agg.PendingAdvancements.Should().BeEmpty();
        agg.ReadyTargets.Should().Contain("FNL-0001----");
    }

    [Fact]
    public void MarkTargetReady_WithNoPending_ReturnsEmpty()
    {
        var agg = MakeAggregate();

        var flushed = agg.MarkTargetReady("FNL-0001----");

        flushed.Should().BeEmpty();
        agg.ReadyTargets.Should().Contain("FNL-0001----");
    }

    [Fact]
    public void MarkTargetReady_Twice_IsIdempotent()
    {
        var agg = MakeAggregate();
        agg.MarkTargetReady("FNL-0001----");

        var flushed = agg.MarkTargetReady("FNL-0001----");

        flushed.Should().BeEmpty();
        agg.ReadyTargets.Should().ContainSingle();
    }
}
