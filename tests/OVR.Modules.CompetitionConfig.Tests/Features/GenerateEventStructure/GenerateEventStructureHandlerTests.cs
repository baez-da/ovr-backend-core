using FluentAssertions;
using MediatR;
using NSubstitute;
using OVR.Modules.CompetitionConfig.Domain;
using OVR.Modules.CompetitionConfig.Features.GenerateEventStructure;
using OVR.Modules.CompetitionConfig.Persistence;
using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.CompetitionConfig.Tests.Features.GenerateEventStructure;

public class GenerateEventStructureHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IUnitRepository _unitRepo = Substitute.For<IUnitRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly BracketGenerator _generator = new();
    private readonly GenerateEventStructureHandler _handler;

    public GenerateEventStructureHandlerTests()
    {
        _handler = new GenerateEventStructureHandler(_eventRepo, _unitRepo, _publisher, _generator);
    }

    private Event ExistingEventWithoutStructure()
    {
        return Event.Create(
            Rsc.Create("BOXM57KG--------------------------"),
            "BOX",
            Gender.FromCode("M"),
            "57KG",
            null,
            "Men's 57kg");
    }

    [Fact]
    public async Task Handle_ValidRequest_Returns15UnitsAndPublishesEvent()
    {
        var evt = ExistingEventWithoutStructure();
        _eventRepo.GetByRscAsync(evt.Id, Arg.Any<CancellationToken>()).Returns(evt);

        var result = await _handler.Handle(
            new GenerateEventStructureCommand(evt.Id, "SingleElimination", 16),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.UnitRscs.Should().HaveCount(15);
        result.Value.Phases.Should().HaveCount(4);
        await _unitRepo.Received(1).AddManyAsync(Arg.Is<IEnumerable<OVR.Modules.CompetitionConfig.Domain.Unit>>(u => u.Count() == 15), Arg.Any<CancellationToken>());
        await _eventRepo.Received(1).UpdateAsync(evt, Arg.Any<CancellationToken>());
        await _publisher.Received(1).Publish(Arg.Any<EventStructureGeneratedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EventNotFound_Returns404Error()
    {
        _eventRepo.GetByRscAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Event?)null);

        var result = await _handler.Handle(
            new GenerateEventStructureCommand("BOXM99KG--------------------------", "SingleElimination", 16),
            CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CompetitionConfig.EventNotFound");
    }

    [Fact]
    public async Task Handle_StructureAlreadyGenerated_Returns409Error()
    {
        var evt = ExistingEventWithoutStructure();
        evt.GenerateStructure(CompetitionFormat.SingleElimination, 4, 1, _generator);
        evt.ClearDomainEvents();
        _eventRepo.GetByRscAsync(evt.Id, Arg.Any<CancellationToken>()).Returns(evt);

        var result = await _handler.Handle(
            new GenerateEventStructureCommand(evt.Id, "SingleElimination", 16),
            CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CompetitionConfig.StructureAlreadyGenerated");
    }

    [Fact]
    public async Task Handle_Size13_RoundsUpAndReturns15Units()
    {
        var evt = ExistingEventWithoutStructure();
        _eventRepo.GetByRscAsync(evt.Id, Arg.Any<CancellationToken>()).Returns(evt);

        var result = await _handler.Handle(
            new GenerateEventStructureCommand(evt.Id, "SingleElimination", 13),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.UnitRscs.Should().HaveCount(15);
        result.Value.Size.Should().Be(13);
    }
}
