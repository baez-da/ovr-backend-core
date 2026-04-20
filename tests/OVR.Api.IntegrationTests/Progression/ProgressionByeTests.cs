using FluentAssertions;
using OVR.Api.IntegrationTests.Progression.Support;
using OVR.SharedKernel.Domain.Events.Integration;

namespace OVR.Api.IntegrationTests.Progression;

/// <summary>
/// Task 26: Bye auto-advance in round 1.
///
/// Size-6 bracket (m=8): QFNL (4 units), SFNL (2 units), FNL- (1 unit).
/// Seed order m=8: [1,8,4,5,2,7,3,6]
/// QFNL pairings with only seeds 1-6 present:
///   QFNL0001: 1 vs 8-bye  → P1 auto-advances
///   QFNL0002: 4 vs 5      → normal
///   QFNL0003: 2 vs 7-bye  → P2 auto-advances
///   QFNL0004: 3 vs 6      → normal
/// Edges: QFNL0001→SFNL0005 slot 1, QFNL0002→SFNL0005 slot 2,
///        QFNL0003→SFNL0006 slot 1, QFNL0004→SFNL0006 slot 2,
///        SFNL0005→FNL-0007 slot 1, SFNL0006→FNL-0007 slot 2.
/// </summary>
public class ProgressionByeTests : IClassFixture<ProgressionWebAppFactory>
{
    private readonly ProgressionWebAppFactory _factory;
    private readonly HttpClient _client;

    public ProgressionByeTests(ProgressionWebAppFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    [Fact]
    public async Task ByeQfnl0001_AutoAdvancesP1_AndFillsSlot1OfSfnl()
    {
        _factory.Events.Reset();

        var fullEventRsc = await ProgressionTestHelpers.CreateBoxEventAsync(_client, "Y6KG");
        var prefix       = ProgressionTestHelpers.EventPrefix(fullEventRsc);

        var qfnl0001 = $"{prefix}QFNL0001----";
        var sfnl0005 = $"{prefix}SFNL0005----";

        const string P1 = "NOC-ESP-Y601"; // seed 1 → QFNL0001 (1 vs 8-bye)
        const string P2 = "NOC-POL-Y602"; // seed 2
        const string P3 = "NOC-GBR-Y603"; // seed 3
        const string P4 = "NOC-FRA-Y604"; // seed 4
        const string P5 = "NOC-ITA-Y605"; // seed 5
        const string P6 = "NOC-USA-Y606"; // seed 6

        // Seeds 1-6 only; seeds 7 and 8 are byes.
        // Entries use fullEventRsc (34-char) to match UnitScheduledEvent.EventRsc.
        await _factory.SeedEntriesAsync(fullEventRsc, new[]
        {
            (P1, "ESP", 1),
            (P2, "POL", 2),
            (P3, "GBR", 3),
            (P4, "FRA", 4),
            (P5, "ITA", 5),
            (P6, "USA", 6)
        });

        await ProgressionTestHelpers.GenerateStructureAsync(_client, fullEventRsc, size: 6);

        // ── 1. Verify 6 edges ─────────────────────────────────────────────────
        var bp = await _factory.GetBracketProgressionAsync(fullEventRsc);
        bp.Should().NotBeNull();
        bp!.Edges.Should().HaveCount(6,
            "m=8 bracket: 4 QFNL→SFNL + 2 SFNL→FNL edges");

        const string session = "Y6BYE";
        await ProgressionTestHelpers.EnsureSessionAsync(_client, session);

        // ── 2. Schedule QFNL0001 (seed 1 vs seed 8-bye) ──────────────────────
        // UnitScheduledEventHandler detects bye and auto-creates Official UnitResult,
        // then publishes UnitResultStartListCreatedEvent + UnitResultOfficialEvent.
        // Progression then fires CompetitorAdvancedEvent for SFNL0005 slot 1.
        await ProgressionTestHelpers.ScheduleUnitAsync(_client, session, qfnl0001, orderInSession: 1);
        await Task.Delay(400);

        // ── 3. QFNL0001 UnitResult must be Official with P1 as winner ─────────
        var qfnl1Result = await _factory.GetUnitResultAsync(qfnl0001);
        qfnl1Result.Should().NotBeNull("bye unit must get a UnitResult immediately on scheduling");
        qfnl1Result!.Status.Should().Be("Official",
            "bye units are auto-officialized without human confirmation");
        qfnl1Result.Decision.Should().NotBeNull();
        qfnl1Result.Decision!.WinnerParticipantId.Should().Be(P1);

        // ── 4. UnitResultStartListCreatedEvent and UnitResultOfficialEvent captured ─
        _factory.Events.OfType<UnitResultStartListCreatedEvent>()
            .Should().Contain(e => e.UnitRsc == qfnl0001,
                "UnitResultStartListCreatedEvent must be published for the bye unit");

        _factory.Events.OfType<UnitResultOfficialEvent>()
            .Should().Contain(e => e.UnitRsc == qfnl0001 && e.WinnerParticipantId == P1,
                "UnitResultOfficialEvent must be published for the bye unit");

        // ── 5. Schedule SFNL0005 — P1 must be placed in slot 1 ───────────────
        // When SFNL0005 is scheduled (UnitResult created), the StartListCreated handler
        // flushes any buffered advancement (or CompetitorAdvancedEvent fires if target was ready).
        await ProgressionTestHelpers.ScheduleUnitAsync(_client, session, sfnl0005, orderInSession: 5);
        await Task.Delay(400);

        var sfnl5Result = await _factory.GetUnitResultAsync(sfnl0005);
        sfnl5Result.Should().NotBeNull("SFNL0005 was just scheduled");
        sfnl5Result!.Competitors
            .Should().ContainSingle(c => c.SortOrder == 1 && c.ParticipantId == P1,
                "P1 must have been advanced from QFNL0001 into SFNL0005 slot 1");
    }
}
