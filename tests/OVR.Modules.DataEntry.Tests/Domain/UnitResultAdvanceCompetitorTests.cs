using FluentAssertions;
using OVR.Modules.DataEntry.Domain;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.DataEntry.Tests.Domain;

public class UnitResultAdvanceCompetitorTests
{
    private static Rsc MakeUnitRsc() =>
        Rsc.Create("BOXM57KG--------------8FNL0001----");

    private static Competitor Red() =>
        new(1, ParticipantId.Create("NOC-ESP-0001"), null, 1,
            Organisation.Create("ESP"), null);

    private static Competitor Blue() =>
        new(2, ParticipantId.Create("NOC-POL-0014"), null, 8,
            Organisation.Create("POL"), null);

    private static UnitResult NewInStartList() =>
        UnitResult.CreateForFirstRound(MakeUnitRsc(), Red(), Blue()).Value;

    // ── AdvanceCompetitor tests ────────────────────────────────────────────────

    [Fact]
    public void AdvanceCompetitor_WhenSlotEmpty_FillsSlotAndReturnsSuccess()
    {
        // Arrange: create a UnitResult in StartList with slot 1 occupied by Red,
        // slot 2 occupied by Blue, then clear slot 2 by replacing Blue's participantId
        // with null — mimic a "seed-only" unit where slot 2 is a bye placeholder.
        // We use AdvanceCompetitor to fill slot 2 with a new participant.

        // Use a fresh UnitResult where we know Red is slot 1. We advance a NEW participant
        // into slot 1 (which currently has Red) — but that would be SlotConflict.
        // Instead, we test a UnitResult that starts with one null-participant slot.
        // Since CreateForFirstRound requires non-null participants, we need to test
        // the method on a UnitResult created specifically for advancement scenarios.
        // The simplest valid test: same participant idempotent no-op is a valid "empty" analogue.
        //
        // Actually let's test the true empty-slot case: we need a UnitResult where
        // one competitor has a null ParticipantId. This requires internal knowledge.
        // We use the ConfirmBye factory (Task 22) — but that's not written yet.
        //
        // Alternative: We can test AdvanceCompetitor via the Hydrate internal method indirectly
        // via a competitor with null participantId... but Hydrate is internal.
        //
        // The cleanest test is: create StartList unit with a real slot-1 competitor.
        // Then call AdvanceCompetitor(slot=2, newParticipant) where slot 2 currently
        // has Blue. But that's a SlotConflict unless newParticipant == Blue.
        //
        // The real empty-slot scenario arises from progression (slot filled after-the-fact).
        // We test it by creating a UnitResult via ConfirmBye-like path in Task 22.
        // For now, we verify the behavior using a UnitResult that has slot 2 with null.
        //
        // We expose this via the Domain's `CreateWithByeSlot` which is Task 22.
        //
        // For this test: advance Red into slot 1 on a unit whose slot-1 is currently EMPTY.
        // We create such a unit by calling CreateByeOfficial isn't available yet.
        //
        // Resolution: Test the empty-slot case by using a unit that was created in StartList
        // with a null-participantId competitor. Since CreateForFirstRound rejects nulls,
        // we will create a helper that uses Hydrate (internal to assembly — use InternalsVisibleTo).
        //
        // Actually: let's just create a valid StartList unit and test via AdvanceCompetitor
        // where slot 2 participant is null. We check InternalsVisibleTo attribute presence.

        // Simplest faithful approach: for "slot empty" the empty slot means the participant
        // in that slot is null. We need a way to construct such a UnitResult.
        // Checking if we can directly call Hydrate from test assembly...
        // The test will FAIL until method exists. Let's write a correct test.
        //
        // We'll use the fact that AdvanceCompetitor on an empty slot should work.
        // We create such a unit via a dedicated test setup that calls into the aggregate.
        //
        // The test: fill slot 1 of a unit that has slot-1 as null-participant.
        // Build a UnitResult via Hydrate with null ParticipantId in slot 1.

        var unitRsc = MakeUnitRsc();
        var redWithNullPid = new Competitor(1, null, null, 1, Organisation.Create("ESP"), null);
        var blue = Blue();

        // Hydrate is internal to the DataEntry module. The test project can call it
        // because [assembly: InternalsVisibleTo("OVR.Modules.DataEntry.Tests")] should exist.
        var ur = UnitResult.Hydrate(
            unitRsc,
            ResultStatus.StartList,
            new[] { redWithNullPid, blue },
            Array.Empty<Period>(),
            decision: null,
            startedAt: null,
            endedAt: null,
            currentPeriodCode: null,
            createdAt: DateTime.UtcNow,
            updatedAt: null);

        var advancingParticipant = ParticipantId.Create("NOC-CUB-0042");

        var result = ur.AdvanceCompetitor(1, advancingParticipant);

        result.IsError.Should().BeFalse();
        ur.Competitors.First(c => c.SortOrder == 1).ParticipantId.Should().Be(advancingParticipant);
    }

    [Fact]
    public void AdvanceCompetitor_WhenSlotHasSameParticipant_IsNoOp()
    {
        var ur = NewInStartList();
        var red = ur.Competitors.First(c => c.SortOrder == 1).ParticipantId!;

        var result = ur.AdvanceCompetitor(1, red);

        result.IsError.Should().BeFalse();
        // State unchanged — still same participant
        ur.Competitors.First(c => c.SortOrder == 1).ParticipantId.Should().Be(red);
    }

    [Fact]
    public void AdvanceCompetitor_WhenSlotHasDifferentParticipant_ReturnsSlotConflict()
    {
        var ur = NewInStartList();
        var intruder = ParticipantId.Create("NOC-CUB-0042");

        var result = ur.AdvanceCompetitor(1, intruder);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("DataEntry.SlotConflict");
    }

    [Fact]
    public void AdvanceCompetitor_WithInvalidSlot_ReturnsInvalidSlot()
    {
        var ur = NewInStartList();
        var pid = ParticipantId.Create("NOC-CUB-0042");

        var slot0 = ur.AdvanceCompetitor(0, pid);
        var slot3 = ur.AdvanceCompetitor(3, pid);

        slot0.IsError.Should().BeTrue();
        slot0.FirstError.Code.Should().Be("DataEntry.InvalidSlot");

        slot3.IsError.Should().BeTrue();
        slot3.FirstError.Code.Should().Be("DataEntry.InvalidSlot");
    }

    [Fact]
    public void AdvanceCompetitor_WhenStateNotStartList_ReturnsUnitNotInStartList()
    {
        var ur = NewInStartList();
        ur.Start(); // transitions to Live

        var pid = ParticipantId.Create("NOC-CUB-0042");
        var result = ur.AdvanceCompetitor(1, pid);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("DataEntry.UnitNotInStartList");
    }
}
