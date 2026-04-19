using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OVR.Modules.CompetitionConfig.Contracts;
using OVR.Modules.DataEntry.Features.CreateUnitResultOnScheduled;
using OVR.Modules.DataEntry.Lineup;
using OVR.Modules.DataEntry.Persistence;
using OVR.Modules.Entries.Contracts;
using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.DataEntry.Tests.Features.CreateUnitResultOnScheduled;

public class UnitScheduledEventHandlerTests
{
    private readonly IUnitResultRepository _repository = Substitute.For<IUnitResultRepository>();
    private readonly IUnitLineupReader _lineupReader = Substitute.For<IUnitLineupReader>();
    private readonly IEntryReader _entryReader = Substitute.For<IEntryReader>();
    private readonly IFirstRoundLineupResolver _resolver = new SeedBasedFirstRoundLineupResolver();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private UnitScheduledEventHandler Handler() => new(
        _repository, _lineupReader, _entryReader, _resolver, _publisher,
        NullLogger<UnitScheduledEventHandler>.Instance);

    // Use a Unit-level RSC that Rsc.Create accepts. Copy the pattern from existing tests.
    private static UnitScheduledEvent Evt() => new(
        UnitRsc:       "BOXM57KG--------------8FNL0001----",
        EventRsc:      "BOXM57KG---------",
        SessionCode:   "S1",
        LocationCode:  "BXR",
        StartTime:     DateTime.UtcNow,
        OrderInSession: 1,
        OrderInLocation: 1,
        ScheduledAt:   DateTime.UtcNow);

    [Fact]
    public async Task Handle_WhenAllInputsValid_CreatesUnitResultAndPublishesEvent()
    {
        _repository.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _lineupReader.GetSeedsForUnit(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((1, 8));
        _entryReader.ListActiveByEventRsc(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<EntryDto>
            {
                new(ParticipantId.Create("NOC-ESP-0001"), 1, Organisation.Create("ESP")),
                new(ParticipantId.Create("NOC-POL-0014"), 8, Organisation.Create("POL"))
            });
        _repository.SaveNewAsync(
            Arg.Any<OVR.Modules.DataEntry.Domain.UnitResult>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await Handler().Handle(Evt(), default);

        await _repository.Received(1).SaveNewAsync(
            Arg.Any<OVR.Modules.DataEntry.Domain.UnitResult>(), Arg.Any<CancellationToken>());
        await _publisher.Received(1).Publish(
            Arg.Any<UnitResultStartListCreatedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUnitResultAlreadyExists_Skips()
    {
        _repository.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await Handler().Handle(Evt(), default);

        await _repository.DidNotReceive().SaveNewAsync(
            Arg.Any<OVR.Modules.DataEntry.Domain.UnitResult>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSeedsMissing_SkipsWithoutError()
    {
        _repository.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _lineupReader.GetSeedsForUnit(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((null, null));

        await Handler().Handle(Evt(), default);

        await _repository.DidNotReceive().SaveNewAsync(
            Arg.Any<OVR.Modules.DataEntry.Domain.UnitResult>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenDuplicateKeyOnInsert_DoesNotPublishEvent()
    {
        // Race: ExistsAsync returned false but a concurrent insert beat us to E11000.
        _repository.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _lineupReader.GetSeedsForUnit(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((1, 8));
        _entryReader.ListActiveByEventRsc(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<EntryDto>
            {
                new(ParticipantId.Create("NOC-ESP-0001"), 1, Organisation.Create("ESP")),
                new(ParticipantId.Create("NOC-POL-0014"), 8, Organisation.Create("POL"))
            });
        _repository.SaveNewAsync(
            Arg.Any<OVR.Modules.DataEntry.Domain.UnitResult>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await Handler().Handle(Evt(), default);

        await _repository.Received(1).SaveNewAsync(
            Arg.Any<OVR.Modules.DataEntry.Domain.UnitResult>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(
            Arg.Any<UnitResultStartListCreatedEvent>(), Arg.Any<CancellationToken>());
    }

    // ── Task 22: Bye auto-OFFICIAL ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenOnlyOneResolvedCompetitor_CreatesUnitResultOfficialImmediately()
    {
        // Only seed 1 has an entry; seed 8 is a bye (no entry found).
        _repository.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _lineupReader.GetSeedsForUnit(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((1, 8));
        _entryReader.ListActiveByEventRsc(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<EntryDto>
            {
                new(ParticipantId.Create("NOC-ESP-0001"), 1, Organisation.Create("ESP"))
                // seed 8 is absent → bye
            });
        _repository.SaveNewAsync(
            Arg.Any<OVR.Modules.DataEntry.Domain.UnitResult>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Capture the aggregate passed to SaveNewAsync.
        OVR.Modules.DataEntry.Domain.UnitResult? saved = null;
        await _repository.SaveNewAsync(
            Arg.Do<OVR.Modules.DataEntry.Domain.UnitResult>(ur => saved = ur),
            Arg.Any<CancellationToken>());

        await Handler().Handle(Evt(), default);

        await _repository.Received(1).SaveNewAsync(
            Arg.Any<OVR.Modules.DataEntry.Domain.UnitResult>(), Arg.Any<CancellationToken>());

        saved.Should().NotBeNull();
        saved!.Status.Should().Be(OVR.Modules.DataEntry.Domain.ResultStatus.Official);
        saved.Competitors.First(c => c.SortOrder == 1).ParticipantId!.Value
            .Should().Be("NOC-ESP-0001");

        // Both StartList and Official events must be published.
        await _publisher.Received(1).Publish(
            Arg.Any<UnitResultStartListCreatedEvent>(), Arg.Any<CancellationToken>());
        await _publisher.Received(1).Publish(
            Arg.Any<UnitResultOfficialEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTwoResolvedCompetitors_BehavesAsMvp3()
    {
        // Two real entries → normal StartList path (unchanged from MVP 3).
        _repository.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _lineupReader.GetSeedsForUnit(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((1, 8));
        _entryReader.ListActiveByEventRsc(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<EntryDto>
            {
                new(ParticipantId.Create("NOC-ESP-0001"), 1, Organisation.Create("ESP")),
                new(ParticipantId.Create("NOC-POL-0014"), 8, Organisation.Create("POL"))
            });
        _repository.SaveNewAsync(
            Arg.Any<OVR.Modules.DataEntry.Domain.UnitResult>(), Arg.Any<CancellationToken>())
            .Returns(true);

        OVR.Modules.DataEntry.Domain.UnitResult? saved = null;
        await _repository.SaveNewAsync(
            Arg.Do<OVR.Modules.DataEntry.Domain.UnitResult>(ur => saved = ur),
            Arg.Any<CancellationToken>());

        await Handler().Handle(Evt(), default);

        saved.Should().NotBeNull();
        saved!.Status.Should().Be(OVR.Modules.DataEntry.Domain.ResultStatus.StartList);

        await _publisher.Received(1).Publish(
            Arg.Any<UnitResultStartListCreatedEvent>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(
            Arg.Any<UnitResultOfficialEvent>(), Arg.Any<CancellationToken>());
    }
}
