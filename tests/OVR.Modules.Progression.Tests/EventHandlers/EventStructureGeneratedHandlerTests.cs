using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OVR.Modules.Progression.Domain;
using OVR.Modules.Progression.EventHandlers;
using OVR.Modules.Progression.Persistence;
using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.Progression;

namespace OVR.Modules.Progression.Tests.EventHandlers;

public class EventStructureGeneratedHandlerTests
{
    private readonly IBracketProgressionRepository _repo = Substitute.For<IBracketProgressionRepository>();
    private readonly ILogger<EventStructureGeneratedHandler> _log = Substitute.For<ILogger<EventStructureGeneratedHandler>>();
    private readonly EventStructureGeneratedHandler _sut;

    public EventStructureGeneratedHandlerTests()
    {
        _sut = new EventStructureGeneratedHandler(_repo, _log);
    }

    private static EventStructureGeneratedEvent MakeEvent(params ProgressionEdge[] edges) =>
        new(
            EventRsc: "EVT123",
            Format: "SingleElimination",
            Size: 4,
            Phases: Array.Empty<PhaseInfo>(),
            UnitRscs: Array.Empty<string>(),
            GeneratedAt: DateTime.UtcNow,
            Edges: edges);

    [Fact]
    public async Task Handle_WhenNotExisting_CreatesAggregate()
    {
        _repo.GetByEventAsync("EVT123", Arg.Any<CancellationToken>()).Returns((BracketProgression?)null);
        _repo.SaveNewAsync(Arg.Any<BracketProgression>(), Arg.Any<CancellationToken>()).Returns(true);

        var evt = MakeEvent(new ProgressionEdge("SFNL0001----", Outcome.W, "FNL-0001----", 1));

        await _sut.Handle(evt, CancellationToken.None);

        await _repo.Received(1).SaveNewAsync(
            Arg.Is<BracketProgression>(b => b.EventRsc == "EVT123" && b.Edges.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAlreadyExists_IsIdempotent()
    {
        var existing = BracketProgression.Create("EVT123", Array.Empty<ProgressionEdge>()).Value;
        _repo.GetByEventAsync("EVT123", Arg.Any<CancellationToken>()).Returns(existing);

        await _sut.Handle(MakeEvent(), CancellationToken.None);

        await _repo.DidNotReceive().SaveNewAsync(Arg.Any<BracketProgression>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSaveNewRacesAndLoses_LogsAndReturns()
    {
        _repo.GetByEventAsync("EVT123", Arg.Any<CancellationToken>()).Returns((BracketProgression?)null);
        _repo.SaveNewAsync(Arg.Any<BracketProgression>(), Arg.Any<CancellationToken>()).Returns(false);

        await _sut.Handle(MakeEvent(), CancellationToken.None);

        // Accept race outcome silently; no throw.
    }
}
