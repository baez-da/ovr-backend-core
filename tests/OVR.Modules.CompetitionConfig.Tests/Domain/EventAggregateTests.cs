using FluentAssertions;
using OVR.Modules.CompetitionConfig.Domain;
using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.CompetitionConfig.Tests.Domain;

public class EventAggregateTests
{
    private readonly BracketGenerator _generator = new();

    private static Event CreateBoxing57Kg() =>
        Event.Create(
            rsc: Rsc.Create("BOXM57KG--------------------------"),
            discipline: "BOX",
            gender: Gender.FromCode("M"),
            eventCode: "57KG",
            modifier: null,
            name: "Men's 57kg");

    [Fact]
    public void Create_SetsPropertiesAndDefaults()
    {
        var evt = CreateBoxing57Kg();

        evt.Id.Should().Be("BOXM57KG--------------------------");
        evt.Discipline.Should().Be("BOX");
        evt.EventCode.Should().Be("57KG");
        evt.Modifier.Should().BeNull();
        evt.Name.Should().Be("Men's 57kg");
        evt.Format.Should().BeNull();
        evt.Size.Should().BeNull();
        evt.Phases.Should().BeEmpty();
        evt.StructureGeneratedAt.Should().BeNull();
    }

    [Fact]
    public void GenerateStructure_Size16_SetsFormatSizeAndPhases()
    {
        var evt = CreateBoxing57Kg();

        var result = evt.GenerateStructure(CompetitionFormat.SingleElimination, size: 16, startUnitNumber: 1, _generator);

        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(15);
        evt.Format.Should().Be(CompetitionFormat.SingleElimination);
        evt.Size.Should().Be(16);
        evt.Phases.Should().HaveCount(4);
        evt.Phases.Select(p => p.Code).Should().Equal("8FNL", "QFNL", "SFNL", "FNL-");
        evt.StructureGeneratedAt.Should().NotBeNull();
    }

    [Fact]
    public void GenerateStructure_ProducesCorrectUnitRscs()
    {
        var evt = CreateBoxing57Kg();

        var result = evt.GenerateStructure(CompetitionFormat.SingleElimination, size: 4, startUnitNumber: 1, _generator);

        result.Value.Select(r => r.Value).Should().Equal(
            "BOXM57KG--------------SFNL0001----",
            "BOXM57KG--------------SFNL0002----",
            "BOXM57KG--------------FNL-0003----");
    }

    [Fact]
    public void GenerateStructure_RaisesDomainEvent()
    {
        var evt = CreateBoxing57Kg();

        evt.GenerateStructure(CompetitionFormat.SingleElimination, size: 4, startUnitNumber: 1, _generator);

        var raised = evt.DomainEvents.OfType<EventStructureGeneratedEvent>().SingleOrDefault();
        raised.Should().NotBeNull();
        raised!.EventRsc.Should().Be("BOXM57KG--------------------------");
        raised.Format.Should().Be("SingleElimination");
        raised.Size.Should().Be(4);
        raised.Phases.Should().HaveCount(2);
        raised.UnitRscs.Should().HaveCount(3);
    }

    [Fact]
    public void GenerateStructure_CalledTwice_ReturnsError()
    {
        var evt = CreateBoxing57Kg();
        evt.GenerateStructure(CompetitionFormat.SingleElimination, size: 4, startUnitNumber: 1, _generator);

        var result = evt.GenerateStructure(CompetitionFormat.SingleElimination, size: 8, startUnitNumber: 1, _generator);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CompetitionConfig.StructureAlreadyGenerated");
    }

    [Fact]
    public void GenerateStructure_WithSizeOutOfRange_ReturnsInvalidSize()
    {
        var evt = CreateBoxing57Kg();

        var result = evt.GenerateStructure(CompetitionFormat.SingleElimination, size: 200, startUnitNumber: 1, _generator);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CompetitionConfig.InvalidSize");
    }

    [Fact]
    public void GenerateStructure_WithUnsupportedFormat_ReturnsUnsupportedFormat()
    {
        var evt = CreateBoxing57Kg();

        var result = evt.GenerateStructure((CompetitionFormat)99, size: 16, startUnitNumber: 1, _generator);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CompetitionConfig.UnsupportedFormat");
    }
}
