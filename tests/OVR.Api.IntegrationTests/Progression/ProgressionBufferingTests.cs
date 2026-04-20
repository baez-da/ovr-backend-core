using FluentAssertions;
using OVR.Api.IntegrationTests.Progression.Support;

namespace OVR.Api.IntegrationTests.Progression;

/// <summary>
/// Task 24: Advancement before target scheduled (buffering path).
///
/// Confirm SFNL0001 before FNL-0003 is scheduled.
/// BracketProgression must buffer the advancement.
/// When FNL-0003 is later scheduled, buffer flushes into slot 1.
/// </summary>
public class ProgressionBufferingTests : IClassFixture<ProgressionWebAppFactory>
{
    private readonly ProgressionWebAppFactory _factory;
    private readonly HttpClient _client;

    public ProgressionBufferingTests(ProgressionWebAppFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    [Fact]
    public async Task AdvancementBuffered_UntilTargetScheduled()
    {
        _factory.Events.Reset();

        var fullEventRsc = await ProgressionTestHelpers.CreateBoxEventAsync(_client, "B4KG");
        var prefix       = ProgressionTestHelpers.EventPrefix(fullEventRsc);

        var sfnl0001 = $"{prefix}SFNL0001----";
        var sfnl0002 = $"{prefix}SFNL0002----";
        var fnl0003  = $"{prefix}FNL-0003----";

        const string P1 = "NOC-ESP-B401";
        const string P2 = "NOC-POL-B402";
        const string P3 = "NOC-GBR-B403";
        const string P4 = "NOC-FRA-B404";

        // Entries use fullEventRsc (34-char) to match UnitScheduledEvent.EventRsc
        await _factory.SeedEntriesAsync(fullEventRsc, new[]
        {
            (P1, "ESP", 1),
            (P2, "POL", 2),
            (P3, "GBR", 3),
            (P4, "FRA", 4)
        });
        await ProgressionTestHelpers.GenerateStructureAsync(_client, fullEventRsc, size: 4);

        const string session = "B4BUFFER";
        await ProgressionTestHelpers.EnsureSessionAsync(_client, session);

        // ── 1. Schedule SFNL0001 and SFNL0002 — but NOT FNL-0003 ─────────────
        await ProgressionTestHelpers.ScheduleUnitAsync(_client, session, sfnl0001, orderInSession: 1);
        await ProgressionTestHelpers.ScheduleUnitAsync(_client, session, sfnl0002, orderInSession: 2);

        // ── 2. Confirm SFNL0001: P1 (seed 1, red in 1v4 pairing) wins ────────
        await ProgressionTestHelpers.ConfirmWinnerByPointsAsync(_client, sfnl0001, redWins: true);
        await Task.Delay(300);

        // ── 3. BracketProgression must buffer P1→FNL-0003 slot 1 ─────────────
        var bp = await _factory.GetBracketProgressionAsync(fullEventRsc);
        bp.Should().NotBeNull();
        bp!.PendingAdvancements.Should().ContainSingle(p =>
            p.TargetUnitRsc == fnl0003 &&
            p.TargetSlot == 1 &&
            p.ParticipantId == P1,
            "advancement must be buffered because FNL-0003 is not yet scheduled");

        // ── 4. UnitResult for FNL-0003 must NOT exist yet ───────────────────
        var fnlBefore = await _factory.GetUnitResultAsync(fnl0003);
        fnlBefore.Should().BeNull("FNL-0003 not yet scheduled → no UnitResult should exist");

        // ── 5. Now schedule FNL-0003 ─────────────────────────────────────────
        await ProgressionTestHelpers.ScheduleUnitAsync(_client, session, fnl0003, orderInSession: 3);
        await Task.Delay(400);

        // ── 6. FNL-0003 UnitResult must have P1 in slot 1 ─────────────────────
        var fnlAfter = await _factory.GetUnitResultAsync(fnl0003);
        fnlAfter.Should().NotBeNull("FNL-0003 was just scheduled → UnitResult must exist");
        fnlAfter!.Competitors
            .Should().ContainSingle(c => c.SortOrder == 1 && c.ParticipantId == P1,
                "pending advancement for P1 must have been flushed into FNL-0003 slot 1");

        // ── 7. Pending buffer must be empty for FNL-0003 ──────────────────────
        var bpAfter = await _factory.GetBracketProgressionAsync(fullEventRsc);
        bpAfter!.PendingAdvancements
            .Should().NotContain(p => p.TargetUnitRsc == fnl0003,
                "after flushing, no pending advancements for FNL-0003 should remain");
    }
}
