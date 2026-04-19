using FluentAssertions;
using OVR.Modules.DataEntry.Lineup;
using OVR.Modules.Entries.Contracts;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.DataEntry.Tests.Lineup;

public class SeedBasedFirstRoundLineupResolverTests
{
    private readonly SeedBasedFirstRoundLineupResolver _resolver = new();

    private static EntryDto E(string pid, string org, int seed) =>
        new(ParticipantId.Create(pid), seed, Organisation.Create(org));

    [Fact]
    public void Resolve_LowerSeedGetsRedCorner()
    {
        var entries = new[]
        {
            E("NOC-ESP-0001", "ESP", 1),
            E("NOC-POL-0014", "POL", 8)
        };

        var result = _resolver.Resolve(seedA: 1, seedB: 8, entries);
        result.IsError.Should().BeFalse();

        var (red, blue) = result.Value;
        red.SortOrder.Should().Be(1);
        red.Seed.Should().Be(1);
        blue.SortOrder.Should().Be(2);
        blue.Seed.Should().Be(8);
    }

    [Fact]
    public void Resolve_ReversedSeedArgs_StillAssignsLowerSeedToRed()
    {
        var entries = new[]
        {
            E("NOC-ESP-0001", "ESP", 1),
            E("NOC-POL-0014", "POL", 8)
        };

        var result = _resolver.Resolve(seedA: 8, seedB: 1, entries);
        result.IsError.Should().BeFalse();

        var (red, _) = result.Value;
        red.Seed.Should().Be(1);
    }

    [Fact]
    public void Resolve_SeedNotFound_ReturnsError()
    {
        var entries = new[] { E("NOC-ESP-0001", "ESP", 1) };

        var result = _resolver.Resolve(seedA: 1, seedB: 8, entries);
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("DataEntry.LineupResolutionFailed");
    }

    [Fact]
    public void Resolve_DuplicateSeed_ReturnsError()
    {
        var entries = new[]
        {
            E("NOC-ESP-0001", "ESP", 1),
            E("NOC-ESP-0002", "ESP", 1),
            E("NOC-POL-0014", "POL", 8)
        };

        var result = _resolver.Resolve(1, 8, entries);
        result.IsError.Should().BeTrue();
    }
}
