using FluentAssertions;
using OVR.Modules.Progression.Domain;
using OVR.SharedKernel.Domain.Progression;

namespace OVR.Modules.Progression.Tests.Domain;

public class BracketProgressionHydrateTests
{
    [Fact]
    public void Hydrate_ReconstitutesAllFields()
    {
        var edges = new[]
        {
            new ProgressionEdge("SFNL0001----", Outcome.W, "FNL-0001----", 1)
        };
        var pending = new[]
        {
            new PendingAdvancement("FNL-0001----", 1, "P1", "SFNL0001----", DateTime.UtcNow)
        };
        var ready = new[] { "SFNL0001----" };
        var createdAt = new DateTime(2026, 4, 18, 10, 0, 0, DateTimeKind.Utc);

        var agg = BracketProgression.Hydrate("EVT123", edges, ready, pending, createdAt);

        agg.EventRsc.Should().Be("EVT123");
        agg.Edges.Should().BeEquivalentTo(edges);
        agg.ReadyTargets.Should().Contain("SFNL0001----");
        agg.PendingAdvancements.Should().BeEquivalentTo(pending);
        agg.CreatedAt.Should().Be(createdAt);
    }
}
