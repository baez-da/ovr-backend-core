using FluentAssertions;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.ParticipantRegistry.Tests.Domain;

public class ParticipantIdTests
{
    [Fact]
    public void Generate_ShouldReturnIdWithLocPrefix()
    {
        var id = ParticipantId.Generate();
        id.Value.Should().StartWith("LOC-");
    }

    [Fact]
    public void Generate_ShouldReturnUniqueIds()
    {
        var id1 = ParticipantId.Generate();
        var id2 = ParticipantId.Generate();
        id1.Value.Should().NotBe(id2.Value);
    }

    [Fact]
    public void Create_ShouldAcceptAnyNonEmptyString()
    {
        var id = ParticipantId.Create("GMS-12345");
        id.Value.Should().Be("GMS-12345");
    }

    [Fact]
    public void Create_ShouldThrowOnNullOrWhitespace()
    {
        var act = () => ParticipantId.Create("");
        act.Should().Throw<ArgumentException>();
    }
}
