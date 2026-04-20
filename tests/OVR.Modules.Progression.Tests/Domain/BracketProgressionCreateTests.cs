using ErrorOr;
using FluentAssertions;
using OVR.Modules.Progression.Domain;
using OVR.SharedKernel.Domain.Progression;

namespace OVR.Modules.Progression.Tests.Domain;

public class BracketProgressionCreateTests
{
    [Fact]
    public void Create_WithValidEdges_ReturnsAggregate()
    {
        var edges = new[]
        {
            new ProgressionEdge("SFNL0001----", Outcome.W, "FNL-0001----", 1),
            new ProgressionEdge("SFNL0002----", Outcome.W, "FNL-0001----", 2)
        };

        var result = BracketProgression.Create("EVT123", edges);

        result.IsError.Should().BeFalse();
        result.Value.EventRsc.Should().Be("EVT123");
        result.Value.Edges.Should().HaveCount(2);
    }

    [Fact]
    public void Create_WithDuplicateSourceOutcome_ReturnsError()
    {
        var edges = new[]
        {
            new ProgressionEdge("SFNL0001----", Outcome.W, "FNL-0001----", 1),
            new ProgressionEdge("SFNL0001----", Outcome.W, "FNL-0002----", 1)
        };

        var result = BracketProgression.Create("EVT123", edges);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Progression.DuplicateEdge");
    }

    [Fact]
    public void Create_WithDuplicateTargetSlot_ReturnsError()
    {
        var edges = new[]
        {
            new ProgressionEdge("SFNL0001----", Outcome.W, "FNL-0001----", 1),
            new ProgressionEdge("SFNL0002----", Outcome.W, "FNL-0001----", 1)
        };

        var result = BracketProgression.Create("EVT123", edges);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Progression.DuplicateTargetSlot");
    }

    [Fact]
    public void Create_WithInvalidSlot_ReturnsError()
    {
        var edges = new[]
        {
            new ProgressionEdge("SFNL0001----", Outcome.W, "FNL-0001----", 3)
        };

        var result = BracketProgression.Create("EVT123", edges);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Progression.InvalidSlot");
    }

    [Fact]
    public void Create_WithEmptyEventRsc_ReturnsError()
    {
        var result = BracketProgression.Create("", Array.Empty<ProgressionEdge>());

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Progression.InvalidEventRsc");
    }

    [Fact]
    public void Create_WithNoEdges_ReturnsAggregate()
    {
        // Edge case: an event with a single-unit "bracket" would have zero progression edges.
        // This should succeed — validation is about edge integrity, not count.
        var result = BracketProgression.Create("EVT123", Array.Empty<ProgressionEdge>());

        result.IsError.Should().BeFalse();
        result.Value.Edges.Should().BeEmpty();
    }
}
