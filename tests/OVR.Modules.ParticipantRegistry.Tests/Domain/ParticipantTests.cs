using FluentAssertions;
using OVR.Modules.ParticipantRegistry.Domain;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.ParticipantRegistry.Tests.Domain;

public class ParticipantTests
{
    private static readonly Description TestDescription =
        Description.Create("John", "Smith", Gender.FromCode("M"), null, Organisation.Create("USA"));

    [Fact]
    public void Create_ShouldGenerateIdWithLocPrefix()
    {
        var participant = Participant.Create(
            ParticipantType.Athlete, TestDescription, null,
            "SMITH John", "SMITH J", "John SMITH", "J. SMITH",
            "SMITH", "SMITH John", "SMITH J", "SMITH John");

        participant.Id.Should().StartWith("LOC-");
        participant.ParticipantId.Value.Should().StartWith("LOC-");
    }

    [Fact]
    public void Create_ShouldRaiseParticipantCreatedEvent()
    {
        var participant = Participant.Create(
            ParticipantType.Athlete, TestDescription, null,
            "SMITH John", "SMITH J", "John SMITH", "J. SMITH",
            "SMITH", "SMITH John", "SMITH J", "SMITH John");

        participant.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void Create_ShouldStoreAllEightNames()
    {
        var participant = Participant.Create(
            ParticipantType.Athlete, TestDescription, null,
            "printName", "printInitialName", "tvName", "tvInitialName",
            "tvFamilyName", "pscbName", "pscbShortName", "pscbLongName");

        participant.PrintName.Should().Be("printName");
        participant.PrintInitialName.Should().Be("printInitialName");
        participant.TvName.Should().Be("tvName");
        participant.TvInitialName.Should().Be("tvInitialName");
        participant.TvFamilyName.Should().Be("tvFamilyName");
        participant.PscbName.Should().Be("pscbName");
        participant.PscbShortName.Should().Be("pscbShortName");
        participant.PscbLongName.Should().Be("pscbLongName");
    }

    [Fact]
    public void Hydrate_ShouldAcceptAnyIdAndNotRaiseEvents()
    {
        var id = ParticipantId.Create("GMS-12345");
        var participant = Participant.Hydrate(
            id, ParticipantType.Athlete, TestDescription, null,
            "p", "pi", "tv", "tvi", "tvf", "pscb", "pscbs", "pscbl",
            DateTime.UtcNow, null);

        participant.Id.Should().Be("GMS-12345");
        participant.DomainEvents.Should().BeEmpty();
    }
}
