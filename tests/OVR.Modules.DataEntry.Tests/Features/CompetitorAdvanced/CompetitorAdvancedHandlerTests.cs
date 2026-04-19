using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OVR.Modules.DataEntry.Domain;
using OVR.Modules.DataEntry.EventHandlers;
using OVR.Modules.DataEntry.Persistence;
using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.DataEntry.Tests.Features.CompetitorAdvanced;

public class CompetitorAdvancedHandlerTests
{
    private readonly IUnitResultRepository _repository = Substitute.For<IUnitResultRepository>();

    private CompetitorAdvancedHandler Handler() => new(
        _repository,
        NullLogger<CompetitorAdvancedHandler>.Instance);

    private static Rsc MakeUnitRsc(string suffix = "0001") =>
        Rsc.Create($"BOXM57KG--------------8FNL{suffix}----");

    private static CompetitorAdvancedEvent MakeEvent(
        string targetUnitRsc,
        int targetSlot,
        string participantId) =>
        new(
            EventRsc: "BOXM57KG---------",
            TargetUnitRsc: targetUnitRsc,
            TargetSlot: targetSlot,
            ParticipantId: participantId,
            SourceUnitRsc: "BOXM57KG--------------8FNL0000----",
            AdvancedAt: DateTime.UtcNow);

    // Builds a StartList UnitResult with slot 1 occupied and slot 2 empty (null participantId).
    private static UnitResult StartListWithEmptySlot2()
    {
        var unitRsc = Rsc.Create("BOXM57KG--------------8FNL0002----");
        var red = new Competitor(1, ParticipantId.Create("NOC-ESP-0001"), null, 1,
            Organisation.Create("ESP"), null);
        var blueEmpty = new Competitor(2, null, null, 8,
            Organisation.Create("POL"), null);

        return UnitResult.Hydrate(
            unitRsc,
            ResultStatus.StartList,
            new[] { red, blueEmpty },
            Array.Empty<Period>(),
            decision: null,
            startedAt: null,
            endedAt: null,
            currentPeriodCode: null,
            createdAt: DateTime.UtcNow,
            updatedAt: null);
    }

    // Builds a Live UnitResult — AdvanceCompetitor should return UnitNotInStartList.
    private static UnitResult LiveUnit()
    {
        var ur = UnitResult.CreateForFirstRound(
            Rsc.Create("BOXM57KG--------------8FNL0003----"),
            new Competitor(1, ParticipantId.Create("NOC-ESP-0001"), null, 1,
                Organisation.Create("ESP"), null),
            new Competitor(2, ParticipantId.Create("NOC-POL-0014"), null, 8,
                Organisation.Create("POL"), null)).Value;
        ur.Start();
        return ur;
    }

    // Builds a StartList UnitResult where slot 1 already has a DIFFERENT participant.
    private static UnitResult StartListWithConflictingSlot1()
    {
        var unitRsc = Rsc.Create("BOXM57KG--------------8FNL0004----");
        var slotConflict = new Competitor(1, ParticipantId.Create("NOC-CUB-0099"), null, 1,
            Organisation.Create("CUB"), null);
        var blue = new Competitor(2, ParticipantId.Create("NOC-POL-0014"), null, 8,
            Organisation.Create("POL"), null);

        return UnitResult.Hydrate(
            unitRsc,
            ResultStatus.StartList,
            new[] { slotConflict, blue },
            Array.Empty<Period>(),
            decision: null,
            startedAt: null,
            endedAt: null,
            currentPeriodCode: null,
            createdAt: DateTime.UtcNow,
            updatedAt: null);
    }

    [Fact]
    public async Task Handle_WithExistingTarget_AdvancesAndReplaces()
    {
        var ur = StartListWithEmptySlot2();
        var targetRsc = ur.UnitRsc.Value;
        var advancingPid = "NOC-GBR-0007";

        _repository.GetAsync(targetRsc, Arg.Any<CancellationToken>())
            .Returns(ur);

        var evt = MakeEvent(targetRsc, 2, advancingPid);

        await Handler().Handle(evt, default);

        await _repository.Received(1).UpdateAsync(ur, Arg.Any<CancellationToken>());
        ur.Competitors.First(c => c.SortOrder == 2).ParticipantId!.Value
            .Should().Be(advancingPid);
    }

    [Fact]
    public async Task Handle_WithMissingTarget_LogsAndReturns()
    {
        var targetRsc = MakeUnitRsc("9999").Value;
        _repository.GetAsync(targetRsc, Arg.Any<CancellationToken>())
            .Returns((UnitResult?)null);

        var evt = MakeEvent(targetRsc, 1, "NOC-GBR-0007");

        await Handler().Handle(evt, default);

        await _repository.DidNotReceive().UpdateAsync(
            Arg.Any<UnitResult>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAdvanceReturnsError_LogsAndDoesNotPersist()
    {
        // Live unit: AdvanceCompetitor returns UnitNotInStartList
        var ur = LiveUnit();
        var targetRsc = ur.UnitRsc.Value;
        _repository.GetAsync(targetRsc, Arg.Any<CancellationToken>())
            .Returns(ur);

        var evt = MakeEvent(targetRsc, 1, "NOC-GBR-0007");

        var act = () => Handler().Handle(evt, default);

        await act.Should().NotThrowAsync();
        await _repository.DidNotReceive().UpdateAsync(
            Arg.Any<UnitResult>(), Arg.Any<CancellationToken>());
    }
}
