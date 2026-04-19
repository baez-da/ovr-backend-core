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

public class UnitResultOfficialHandlerTests
{
    private const string EventRsc = "BOXM54KG--------------";       // 22 chars
    private const string SourceUnit = EventRsc + "SFNL0001----";   // 34 chars
    private const string TargetUnit = EventRsc + "FNL-0001----";   // 34 chars

    private readonly IBracketProgressionRepository _repo = Substitute.For<IBracketProgressionRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly ILogger<UnitResultOfficialHandler> _log = Substitute.For<ILogger<UnitResultOfficialHandler>>();
    private readonly UnitResultOfficialHandler _sut;

    public UnitResultOfficialHandlerTests()
    {
        _sut = new UnitResultOfficialHandler(_repo, _publisher, _log);
    }

    private BracketProgression Aggregate(bool markReady)
    {
        var agg = BracketProgression.Create(EventRsc, new[]
        {
            new ProgressionEdge(SourceUnit, Outcome.W, TargetUnit, 1)
        }).Value;
        if (markReady) agg.MarkTargetReady(TargetUnit);
        return agg;
    }

    private static UnitResultOfficialEvent MakeEvent(string unit, string? winner) =>
        new(unit, winner, "WP", "RM_POINTS", "3:0", null, null, DateTime.UtcNow);

    [Fact]
    public async Task Handle_WithReadyTargetAndWinner_PublishesCompetitorAdvanced()
    {
        _repo.GetByEventAsync(EventRsc, Arg.Any<CancellationToken>())
            .Returns(Aggregate(markReady: true));

        await _sut.Handle(MakeEvent(SourceUnit, "P1"), CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<CompetitorAdvancedEvent>(e =>
                e.EventRsc == EventRsc &&
                e.TargetUnitRsc == TargetUnit &&
                e.TargetSlot == 1 &&
                e.ParticipantId == "P1" &&
                e.SourceUnitRsc == SourceUnit),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithTargetNotReady_DoesNotPublishAndPersists()
    {
        var agg = Aggregate(markReady: false);
        _repo.GetByEventAsync(EventRsc, Arg.Any<CancellationToken>()).Returns(agg);

        await _sut.Handle(MakeEvent(SourceUnit, "P1"), CancellationToken.None);

        await _publisher.DidNotReceive().Publish(Arg.Any<CompetitorAdvancedEvent>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).ReplaceAsync(agg, Arg.Any<CancellationToken>());
        agg.PendingAdvancements.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_WithNullWinner_PublishesProgressionSkipped()
    {
        _repo.GetByEventAsync(EventRsc, Arg.Any<CancellationToken>())
            .Returns(Aggregate(markReady: true));

        await _sut.Handle(MakeEvent(SourceUnit, winner: null), CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<ProgressionSkippedEvent>(e =>
                e.SourceUnitRsc == SourceUnit && e.Reason == "NoWinner"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithTerminalSourceAndWinner_PublishesEventCompleted()
    {
        _repo.GetByEventAsync(EventRsc, Arg.Any<CancellationToken>())
            .Returns(Aggregate(markReady: true));

        await _sut.Handle(MakeEvent(TargetUnit, "PChampion"), CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<EventProgressionCompletedEvent>(e =>
                e.EventRsc == EventRsc &&
                e.FinalUnitRsc == TargetUnit &&
                e.ChampionParticipantId == "PChampion"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithMissingAggregate_LogsAndReturns()
    {
        _repo.GetByEventAsync(EventRsc, Arg.Any<CancellationToken>())
            .Returns((BracketProgression?)null);

        await _sut.Handle(MakeEvent(SourceUnit, "P1"), CancellationToken.None);

        await _publisher.DidNotReceive().Publish(Arg.Any<CompetitorAdvancedEvent>(), Arg.Any<CancellationToken>());
    }
}
