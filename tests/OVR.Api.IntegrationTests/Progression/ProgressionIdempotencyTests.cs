using FluentAssertions;
using OVR.Api.IntegrationTests.Progression.Support;
using OVR.SharedKernel.Domain.Events.Integration;

namespace OVR.Api.IntegrationTests.Progression;

/// <summary>
/// Task 27: Idempotent re-emission.
///
/// 1. Schedule + confirm SFNL0001 (P1 wins).
/// 2. Schedule FNL-0003 (target ready).
/// 3. Manually publish a second UnitResultOfficialEvent for SFNL0001 (same winner).
/// 4. FNL-0003 slot 1 still has exactly one P1.
/// 5. BracketProgression buffer has no duplicate pending entries.
/// </summary>
public class ProgressionIdempotencyTests : IClassFixture<ProgressionWebAppFactory>
{
    private readonly ProgressionWebAppFactory _factory;
    private readonly HttpClient _client;

    public ProgressionIdempotencyTests(ProgressionWebAppFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    [Fact]
    public async Task ReEmittedOfficialEvent_DoesNotDuplicateSlotOrBuffer()
    {
        _factory.Events.Reset();

        var fullEventRsc = await ProgressionTestHelpers.CreateBoxEventAsync(_client, "I4KG");
        var prefix       = ProgressionTestHelpers.EventPrefix(fullEventRsc);

        var sfnl0001 = $"{prefix}SFNL0001----";
        var sfnl0002 = $"{prefix}SFNL0002----";
        var fnl0003  = $"{prefix}FNL-0003----";

        const string P1 = "NOC-ESP-I401";

        // Entries use fullEventRsc (34-char) to match UnitScheduledEvent.EventRsc
        await _factory.SeedEntriesAsync(fullEventRsc, new[]
        {
            (P1,             "ESP", 1),
            ("NOC-POL-I402", "POL", 2),
            ("NOC-GBR-I403", "GBR", 3),
            ("NOC-FRA-I404", "FRA", 4)
        });
        await ProgressionTestHelpers.GenerateStructureAsync(_client, fullEventRsc, size: 4);

        const string session = "I4IDEM";
        await ProgressionTestHelpers.EnsureSessionAsync(_client, session);

        // ── 1. Schedule and confirm SFNL0001 (P1, seed 1, red wins) ──────────
        await ProgressionTestHelpers.ScheduleUnitAsync(_client, session, sfnl0001, orderInSession: 1);
        await ProgressionTestHelpers.ScheduleUnitAsync(_client, session, sfnl0002, orderInSession: 2);
        await ProgressionTestHelpers.ConfirmWinnerByPointsAsync(_client, sfnl0001, redWins: true);
        await Task.Delay(300);

        // ── 2. Schedule FNL-0003 so target is ready ───────────────────────────
        await ProgressionTestHelpers.ScheduleUnitAsync(_client, session, fnl0003, orderInSession: 3);
        await Task.Delay(300);

        // Verify P1 is in slot 1 after normal flow
        var fnlBefore = await _factory.GetUnitResultAsync(fnl0003);
        fnlBefore.Should().NotBeNull();
        fnlBefore!.Competitors
            .Count(c => c.SortOrder == 1 && c.ParticipantId == P1)
            .Should().Be(1, "P1 must be in slot 1 after first confirmation");

        // ── 3. Re-publish the same UnitResultOfficialEvent ────────────────────
        var dupeEvent = new UnitResultOfficialEvent(
            UnitRsc: sfnl0001,
            WinnerParticipantId: P1,
            ResultCode: "Wp",
            ResultType: "Points",
            DecisionMark: "3:0",
            StoppageRound: null,
            StoppageTime: null,
            ConfirmedAt: DateTime.UtcNow);

        await ProgressionTestHelpers.PublishEventAsync(_factory, dupeEvent);
        await Task.Delay(300);

        // ── 4. FNL-0003 slot 1 still has exactly one P1 ──────────────────────
        var fnlAfter = await _factory.GetUnitResultAsync(fnl0003);
        fnlAfter.Should().NotBeNull();
        fnlAfter!.Competitors
            .Count(c => c.SortOrder == 1 && c.ParticipantId == P1)
            .Should().Be(1,
                "re-emitting the same official event must not duplicate P1 in slot 1");

        // ── 5. No pending entries for FNL-0003 ────────────────────────────────
        // FNL-0003 is already ready (StartList exists), so RecordAdvancement returns Ready
        // (not Buffered) on re-emit. CompetitorAdvancedEvent fires again, but AdvanceCompetitor
        // is idempotent for the same slot/participant combination.
        var bp = await _factory.GetBracketProgressionAsync(fullEventRsc);
        bp.Should().NotBeNull();
        bp!.PendingAdvancements
            .Where(p => p.TargetUnitRsc == fnl0003)
            .Should().BeEmpty(
                "FNL-0003 is already ready, so re-emitting must not create pending entries");
    }
}
