using FluentAssertions;
using OVR.Api.IntegrationTests.Progression.Support;
using OVR.SharedKernel.Domain.Events.Integration;

namespace OVR.Api.IntegrationTests.Progression;

/// <summary>
/// Task 25: DKO path emits ProgressionSkipped.
///
/// Confirm SFNL0001 with DKO (no winner).
/// Progression emits ProgressionSkippedEvent(Reason="NoWinner").
/// FNL-0003 slot 1 stays empty; confirming SFNL0002 fills slot 2.
/// </summary>
public class ProgressionDkoTests : IClassFixture<ProgressionWebAppFactory>
{
    private readonly ProgressionWebAppFactory _factory;
    private readonly HttpClient _client;

    public ProgressionDkoTests(ProgressionWebAppFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    [Fact]
    public async Task DkoSfnl_EmitsSkipped_AndFinalSlot1IsEmpty()
    {
        _factory.Events.Reset();

        var fullEventRsc = await ProgressionTestHelpers.CreateBoxEventAsync(_client, "D4KG");
        var prefix       = ProgressionTestHelpers.EventPrefix(fullEventRsc);

        var sfnl0001 = $"{prefix}SFNL0001----";
        var sfnl0002 = $"{prefix}SFNL0002----";
        var fnl0003  = $"{prefix}FNL-0003----";

        const string P2 = "NOC-POL-D402"; // seed 2
        const string P3 = "NOC-GBR-D403"; // seed 3

        // Entries use fullEventRsc (34-char) to match UnitScheduledEvent.EventRsc
        await _factory.SeedEntriesAsync(fullEventRsc, new[]
        {
            ("NOC-ESP-D401", "ESP", 1),
            (P2,             "POL", 2),
            (P3,             "GBR", 3),
            ("NOC-FRA-D404", "FRA", 4)
        });
        await ProgressionTestHelpers.GenerateStructureAsync(_client, fullEventRsc, size: 4);

        const string session = "D4DKO";
        await ProgressionTestHelpers.EnsureSessionAsync(_client, session);
        await ProgressionTestHelpers.ScheduleUnitAsync(_client, session, sfnl0001, orderInSession: 1);
        await ProgressionTestHelpers.ScheduleUnitAsync(_client, session, sfnl0002, orderInSession: 2);
        await ProgressionTestHelpers.ScheduleUnitAsync(_client, session, fnl0003,  orderInSession: 3);

        // ── 1. Confirm SFNL0001 with DKO ─────────────────────────────────────
        await ProgressionTestHelpers.ConfirmDkoAsync(_client, sfnl0001);
        await Task.Delay(300);

        // ── 2. ProgressionSkippedEvent captured ───────────────────────────────
        var skipped = _factory.Events.OfType<ProgressionSkippedEvent>();
        skipped.Should().ContainSingle(e =>
            e.SourceUnitRsc == sfnl0001 && e.Reason == "NoWinner",
            "DKO result must emit ProgressionSkippedEvent with Reason='NoWinner'");

        // ── 3. FNL-0003 slot 1 stays empty ───────────────────────────────────
        var fnlAfterDko = await _factory.GetUnitResultAsync(fnl0003);
        fnlAfterDko.Should().NotBeNull("FNL-0003 was scheduled so UnitResult must exist");
        fnlAfterDko!.Competitors
            .Should().NotContain(c => c.SortOrder == 1 && c.ParticipantId != null,
                "slot 1 must remain empty because SFNL0001 had no winner");

        // ── 4. Confirm SFNL0002: P3 (blue / seed 3) wins ─────────────────────
        // SFNL0002 pairing: seed 2 vs seed 3. seed2 < seed3 → P2=red, P3=blue.
        await ProgressionTestHelpers.ConfirmWinnerByPointsAsync(_client, sfnl0002, redWins: false);
        await Task.Delay(300);

        // ── 5. FNL-0003 slot 2 must have P3 ──────────────────────────────────
        var fnlFinal = await _factory.GetUnitResultAsync(fnl0003);
        fnlFinal!.Competitors
            .Should().ContainSingle(c => c.SortOrder == 2 && c.ParticipantId == P3,
                "P3 (winner of SFNL0002) must be placed in FNL-0003 slot 2");
    }
}
