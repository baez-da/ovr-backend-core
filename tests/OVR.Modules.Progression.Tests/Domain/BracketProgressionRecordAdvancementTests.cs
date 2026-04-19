using FluentAssertions;
using OVR.Modules.Progression.Domain;
using OVR.SharedKernel.Domain.Progression;

namespace OVR.Modules.Progression.Tests.Domain;

public class BracketProgressionRecordAdvancementTests
{
    private static BracketProgression MakeAggregate() =>
        BracketProgression.Create("EVT123", new[]
        {
            new ProgressionEdge("SFNL0001----", Outcome.W, "FNL-0001----", 1),
            new ProgressionEdge("SFNL0002----", Outcome.W, "FNL-0001----", 2)
        }).Value;

    [Fact]
    public void RecordAdvancement_WithEdgeAndTargetReady_ReturnsReady()
    {
        var agg = MakeAggregate();
        agg.MarkTargetReady("FNL-0001----");

        var outcome = agg.RecordAdvancement("SFNL0001----", Outcome.W, "P1");

        outcome.Should().BeOfType<AdvancementOutcome.Ready>()
            .Which.ParticipantId.Should().Be("P1");
    }

    [Fact]
    public void RecordAdvancement_WithEdgeAndTargetNotReady_ReturnsBufferedAndStoresPending()
    {
        var agg = MakeAggregate();

        var outcome = agg.RecordAdvancement("SFNL0001----", Outcome.W, "P1");

        outcome.Should().BeOfType<AdvancementOutcome.Buffered>();
        agg.PendingAdvancements.Should().ContainSingle(p =>
            p.ParticipantId == "P1" && p.TargetUnitRsc == "FNL-0001----" && p.TargetSlot == 1);
    }

    [Fact]
    public void RecordAdvancement_WithNoOutgoingEdge_ReturnsTerminal()
    {
        var agg = MakeAggregate();

        var outcome = agg.RecordAdvancement("FNL-0001----", Outcome.W, "PChampion");

        outcome.Should().BeOfType<AdvancementOutcome.Terminal>()
            .Which.ChampionParticipantId.Should().Be("PChampion");
    }

    [Fact]
    public void RecordAdvancement_WithNullWinner_ReturnsSkipped()
    {
        var agg = MakeAggregate();

        var outcome = agg.RecordAdvancement("SFNL0001----", Outcome.W, participantId: null);

        outcome.Should().BeOfType<AdvancementOutcome.Skipped>()
            .Which.Reason.Should().Be("NoWinner");
        agg.PendingAdvancements.Should().BeEmpty();
    }

    [Fact]
    public void RecordAdvancement_SameSourceTwice_DoesNotDuplicatePending()
    {
        var agg = MakeAggregate();

        agg.RecordAdvancement("SFNL0001----", Outcome.W, "P1");
        agg.RecordAdvancement("SFNL0001----", Outcome.W, "P1");

        agg.PendingAdvancements.Should().ContainSingle();
    }
}
