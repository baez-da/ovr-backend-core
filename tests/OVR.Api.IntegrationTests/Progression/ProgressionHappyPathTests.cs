using FluentAssertions;
using OVR.Api.IntegrationTests.Progression.Support;
using OVR.SharedKernel.Domain.Events.Integration;

namespace OVR.Api.IntegrationTests.Progression;

/// <summary>
/// Task 23: Happy-path bracket of 4 end-to-end.
///
/// Size-4 bracket (m=4) → phases: SFNL (2 units), FNL- (1 unit).
/// Seed order for m=4: [1,4,2,3] → pairings: SFNL0001(1 vs 4), SFNL0002(2 vs 3).
/// Unit numbers (startUnitNumber=1): SFNL 0001-0002, FNL- 0003.
/// Edges: SFNL0001→FNL-0003 slot 1, SFNL0002→FNL-0003 slot 2.
/// </summary>
public class ProgressionHappyPathTests : IClassFixture<ProgressionWebAppFactory>
{
    private readonly ProgressionWebAppFactory _factory;
    private readonly HttpClient _client;

    public ProgressionHappyPathTests(ProgressionWebAppFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    [Fact]
    public async Task BracketOf4_HappyPath_ChampionDetermined()
    {
        _factory.Events.Reset();

        // ── 1. Create event ───────────────────────────────────────────────────
        // fullEventRsc is the 34-char RSC returned by the API.
        // The same 34-char value is what BracketProgression uses as its _id.
        var fullEventRsc = await ProgressionTestHelpers.CreateBoxEventAsync(_client, "H4KG");
        var prefix       = ProgressionTestHelpers.EventPrefix(fullEventRsc); // 22 chars for building unit RSCs

        var sfnl0001 = $"{prefix}SFNL0001----";
        var sfnl0002 = $"{prefix}SFNL0002----";
        var fnl0003  = $"{prefix}FNL-0003----";

        const string P1 = "NOC-ESP-H401"; // seed 1
        const string P2 = "NOC-POL-H402"; // seed 2
        const string P3 = "NOC-GBR-H403"; // seed 3
        const string P4 = "NOC-FRA-H404"; // seed 4

        // ── 2. Seed 4 entries ────────────────────────────────────────────────
        // Entries must use fullEventRsc (34 chars) because the Scheduling module emits
        // UnitScheduledEvent.EventRsc = Rsc.AtEventLevel().Value (34 chars).
        await _factory.SeedEntriesAsync(fullEventRsc, new[]
        {
            (P1, "ESP", 1),
            (P2, "POL", 2),
            (P3, "GBR", 3),
            (P4, "FRA", 4)
        });

        // ── 3. Generate structure (size=4) ───────────────────────────────────
        await ProgressionTestHelpers.GenerateStructureAsync(_client, fullEventRsc, size: 4);

        // ── 4. Verify BracketProgression (keyed by fullEventRsc) has 2 edges ─
        var bp = await _factory.GetBracketProgressionAsync(fullEventRsc);
        bp.Should().NotBeNull("EventStructureGeneratedHandler must create BracketProgression");
        bp!.Edges.Should().HaveCount(2);
        bp.Edges.Should().ContainSingle(e =>
            e.SourceUnitRsc == sfnl0001 && e.TargetUnitRsc == fnl0003 && e.TargetSlot == 1);
        bp.Edges.Should().ContainSingle(e =>
            e.SourceUnitRsc == sfnl0002 && e.TargetUnitRsc == fnl0003 && e.TargetSlot == 2);

        // ── 5. Schedule all three units ───────────────────────────────────────
        const string session = "H4HAPPY";
        await ProgressionTestHelpers.EnsureSessionAsync(_client, session);
        await ProgressionTestHelpers.ScheduleUnitAsync(_client, session, sfnl0001, orderInSession: 1);
        await ProgressionTestHelpers.ScheduleUnitAsync(_client, session, sfnl0002, orderInSession: 2);
        await ProgressionTestHelpers.ScheduleUnitAsync(_client, session, fnl0003,  orderInSession: 3);

        // ── 6. Confirm SFNL0001: P1 (seed 1, red corner in 1v4 pairing) wins ─
        await ProgressionTestHelpers.ConfirmWinnerByPointsAsync(_client, sfnl0001, redWins: true);
        await Task.Delay(300);

        // ── 7. FNL-0003 slot 1 should now have P1 ────────────────────────────
        var fnlAfterSfnl1 = await _factory.GetUnitResultAsync(fnl0003);
        fnlAfterSfnl1.Should().NotBeNull("FNL-0003 was scheduled so UnitResult must exist");
        fnlAfterSfnl1!.Competitors
            .Should().ContainSingle(c => c.SortOrder == 1 && c.ParticipantId == P1,
                "P1 (seed-1, winner of SFNL0001) must be placed in slot 1 of FNL-0003");

        // ── 8. Confirm SFNL0002: P3 (seed 3, blue corner in 2v3 pairing) wins ─
        // seed2 < seed3 → P2=red (sortOrder=1), P3=blue (sortOrder=2). P3 wins.
        await ProgressionTestHelpers.ConfirmWinnerByPointsAsync(_client, sfnl0002, redWins: false);
        await Task.Delay(300);

        var fnlAfterSfnl2 = await _factory.GetUnitResultAsync(fnl0003);
        fnlAfterSfnl2!.Competitors
            .Should().ContainSingle(c => c.SortOrder == 2 && c.ParticipantId == P3,
                "P3 (seed-3, winner of SFNL0002) must be placed in slot 2 of FNL-0003");

        // ── 9. Confirm FNL-0003: P1 (red) wins → EventProgressionCompletedEvent ─
        await ProgressionTestHelpers.ConfirmWinnerByPointsAsync(_client, fnl0003, redWins: true);
        await Task.Delay(300);

        var completed = _factory.Events.OfType<EventProgressionCompletedEvent>();
        completed.Should().ContainSingle(e =>
            e.EventRsc == fullEventRsc && e.ChampionParticipantId == P1,
            "confirming the final unit must fire EventProgressionCompletedEvent with P1 as champion");
    }
}
