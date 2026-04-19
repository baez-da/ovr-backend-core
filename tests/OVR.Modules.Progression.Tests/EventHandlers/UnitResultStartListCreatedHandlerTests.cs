using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OVR.Modules.Progression.Domain;
using OVR.Modules.Progression.EventHandlers;
using OVR.Modules.Progression.Persistence;
using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.Progression;

namespace OVR.Modules.Progression.Tests.EventHandlers;

public class UnitResultStartListCreatedHandlerTests
{
    private const string EventRsc = "BOXM54KG--------------";       // 22 chars
    private const string SourceUnit = EventRsc + "SFNL0001----";   // 34 chars
    private const string TargetUnit = EventRsc + "FNL-0001----";   // 34 chars

    private readonly IBracketProgressionRepository _repo = Substitute.For<IBracketProgressionRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly ILogger<UnitResultStartListCreatedHandler> _log = Substitute.For<ILogger<UnitResultStartListCreatedHandler>>();
    private readonly UnitResultStartListCreatedHandler _sut;

    public UnitResultStartListCreatedHandlerTests()
    {
        _sut = new UnitResultStartListCreatedHandler(_repo, _publisher, _log);
    }

    private static UnitResultStartListCreatedEvent MakeEvent(string unit) =>
        new(unit, EventRsc, Array.Empty<CompetitorSnapshot>(), DateTime.UtcNow);

    [Fact]
    public async Task Handle_WithBufferedPending_PublishesAdvancement()
    {
        var agg = BracketProgression.Create(EventRsc, new[]
        {
            new ProgressionEdge(SourceUnit, Outcome.W, TargetUnit, 1)
        }).Value;
        agg.RecordAdvancement(SourceUnit, Outcome.W, "P1");
        _repo.GetByEventAsync(EventRsc, Arg.Any<CancellationToken>()).Returns(agg);

        await _sut.Handle(MakeEvent(TargetUnit), CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<CompetitorAdvancedEvent>(e =>
                e.TargetUnitRsc == TargetUnit && e.TargetSlot == 1 && e.ParticipantId == "P1"),
            Arg.Any<CancellationToken>());
        await _repo.Received(1).ReplaceAsync(agg, Arg.Any<CancellationToken>());
        agg.PendingAdvancements.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithNoPending_MarksReadyAndReturns()
    {
        var agg = BracketProgression.Create(EventRsc, new[]
        {
            new ProgressionEdge(SourceUnit, Outcome.W, TargetUnit, 1)
        }).Value;
        _repo.GetByEventAsync(EventRsc, Arg.Any<CancellationToken>()).Returns(agg);

        await _sut.Handle(MakeEvent(TargetUnit), CancellationToken.None);

        await _publisher.DidNotReceive().Publish(Arg.Any<CompetitorAdvancedEvent>(), Arg.Any<CancellationToken>());
        agg.ReadyTargets.Should().Contain(TargetUnit);
    }

    [Fact]
    public async Task Handle_WithMissingAggregate_LogsAndReturns()
    {
        _repo.GetByEventAsync(EventRsc, Arg.Any<CancellationToken>())
            .Returns((BracketProgression?)null);

        await _sut.Handle(MakeEvent(TargetUnit), CancellationToken.None);

        await _publisher.DidNotReceive().Publish(Arg.Any<CompetitorAdvancedEvent>(), Arg.Any<CancellationToken>());
    }
}
