using System.Net.Http.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OVR.Modules.CompetitionConfig.Persistence;
using OVR.SharedKernel.Domain.Events.Integration;

namespace OVR.Api.IntegrationTests.Progression.Support;

/// <summary>
/// Reusable HTTP workflow helpers shared across all Progression integration tests.
/// </summary>
public static class ProgressionTestHelpers
{
    // ---------------------------------------------------------------
    // Event + Structure
    // ---------------------------------------------------------------

    /// <summary>
    /// Creates a Boxing event (discipline=BOX, gender=M) via HTTP
    /// and returns the full 34-char RSC from the Location header.
    /// This RSC is used both for API calls and as the BracketProgression key.
    /// </summary>
    public static async Task<string> CreateBoxEventAsync(HttpClient client, string eventCode)
    {
        var body = new
        {
            discipline = "BOX",
            gender = "M",
            eventCode,
            modifier = (string?)null,
            name = $"Men's {eventCode}"
        };

        var resp = await client.PostAsJsonAsync("/api/competition-config/events", body);
        resp.EnsureSuccessStatusCode();
        // Location is .../events/{34-char-rsc}
        return resp.Headers.Location!.OriginalString.Split('/').Last();
    }

    /// <summary>
    /// Returns the 22-char event-level prefix of a full 34-char RSC (for building unit RSCs).
    /// Unit RSCs are: eventPrefix(22) + phase(4) + unitNumber(4 digits) + "----"
    /// </summary>
    public static string EventPrefix(string fullEventRsc) => fullEventRsc[..22];

    /// <summary>
    /// Calls generate-structure and returns the response JSON string.
    /// </summary>
    public static async Task<string> GenerateStructureAsync(
        HttpClient client, string eventRsc, int size)
    {
        var body = new { format = "SingleElimination", size, startUnitNumber = 1 };
        var resp = await client.PostAsJsonAsync(
            $"/api/competition-config/events/{eventRsc}/generate-structure", body);
        resp.IsSuccessStatusCode.Should().BeTrue(
            $"generate-structure failed: {await resp.Content.ReadAsStringAsync()}");
        return await resp.Content.ReadAsStringAsync();
    }

    // ---------------------------------------------------------------
    // Scheduling
    // ---------------------------------------------------------------

    /// <summary>
    /// Creates a scheduling session and returns the session code.
    /// </summary>
    public static async Task<string> EnsureSessionAsync(HttpClient client, string sessionCode)
    {
        var body = new
        {
            code = sessionCode,
            venueCode = "AXC",
            name = $"Session {sessionCode}",
            startDate = "2026-04-20T10:00:00Z",
            endDate = "2026-04-20T18:00:00Z",
            leadin = "00:05:00"
        };
        var resp = await client.PostAsJsonAsync("/api/scheduling/sessions", body);
        // 201 or 409 (already exists) are both fine
        (resp.IsSuccessStatusCode || (int)resp.StatusCode == 409).Should().BeTrue();
        return sessionCode;
    }

    /// <summary>
    /// Schedules a unit into the given session. Each unit gets a distinct time slot.
    /// </summary>
    public static async Task ScheduleUnitAsync(
        HttpClient client, string sessionCode, string unitRsc, int orderInSession)
    {
        // Stagger start times to avoid collision at the same location+time.
        var hour = 10 + orderInSession;
        var body = new
        {
            unitRsc,
            locationCode = "AXC",
            startTime = $"2026-04-20T{hour:D2}:00:00Z",
            orderInSession,
            orderInLocation = orderInSession
        };
        var resp = await client.PostAsJsonAsync(
            $"/api/scheduling/sessions/{sessionCode}/schedule-unit", body);
        resp.IsSuccessStatusCode.Should().BeTrue(
            $"schedule-unit {unitRsc} failed ({resp.StatusCode}): {await resp.Content.ReadAsStringAsync()}");
    }

    // ---------------------------------------------------------------
    // DataEntry: confirm a winner via points (3:0 unanimous)
    // ---------------------------------------------------------------

    /// <summary>
    /// Starts a unit, scores three rounds unanimously for the winner, and confirms.
    /// The winner must be the participant occupying sortOrder=1 (red corner).
    /// Pass <c>redWins=false</c> to make the blue corner win instead.
    /// </summary>
    public static async Task ConfirmWinnerByPointsAsync(
        HttpClient client, string unitRsc, bool redWins = true)
    {
        var startResp = await client.PostAsync(
            $"/api/data-entry/unit-results/{unitRsc}/start", content: null);
        startResp.IsSuccessStatusCode.Should().BeTrue(
            $"start failed for {unitRsc}: {await startResp.Content.ReadAsStringAsync()}");

        var (homeScore, awayScore) = redWins ? (10, 9) : (9, 10);
        var scorecards = new[]
        {
            new { JudgePos = "J1", HomeScore = homeScore, AwayScore = awayScore },
            new { JudgePos = "J2", HomeScore = homeScore, AwayScore = awayScore },
            new { JudgePos = "J3", HomeScore = homeScore, AwayScore = awayScore }
        };
        var scoreBody = new { Scorecards = scorecards };

        foreach (var period in new[] { "R1", "R2", "R3" })
        {
            var scoreResp = await client.PostAsJsonAsync(
                $"/api/data-entry/unit-results/{unitRsc}/periods/{period}/score", scoreBody);
            scoreResp.IsSuccessStatusCode.Should().BeTrue(
                $"score period {period} for {unitRsc} failed: {await scoreResp.Content.ReadAsStringAsync()}");
        }

        var confirmResp = await client.PostAsync(
            $"/api/data-entry/unit-results/{unitRsc}/confirm", content: null);
        confirmResp.IsSuccessStatusCode.Should().BeTrue(
            $"confirm failed for {unitRsc}: {await confirmResp.Content.ReadAsStringAsync()}");
    }

    /// <summary>
    /// Confirms a unit with a DKO result (no winner) by using a stoppage with null winner.
    /// </summary>
    public static async Task ConfirmDkoAsync(HttpClient client, string unitRsc)
    {
        var startResp = await client.PostAsync(
            $"/api/data-entry/unit-results/{unitRsc}/start", content: null);
        startResp.IsSuccessStatusCode.Should().BeTrue(
            $"start failed for {unitRsc}: {await startResp.Content.ReadAsStringAsync()}");

        // Score R1 so we have at least one period (avoid validator rejection)
        var scoreBody = new
        {
            Scorecards = new[]
            {
                new { JudgePos = "J1", HomeScore = 10, AwayScore = 10 },
                new { JudgePos = "J2", HomeScore = 10, AwayScore = 10 },
                new { JudgePos = "J3", HomeScore = 10, AwayScore = 10 }
            }
        };
        var scoreResp = await client.PostAsJsonAsync(
            $"/api/data-entry/unit-results/{unitRsc}/periods/R1/score", scoreBody);
        scoreResp.IsSuccessStatusCode.Should().BeTrue(
            $"score R1 for DKO {unitRsc} failed: {await scoreResp.Content.ReadAsStringAsync()}");

        // Dko = "double knockout" — finish via stoppage with no winner
        var stoppageBody = new
        {
            ResultCode = "Dko",
            StoppageRound = "R1",
            StoppageTime = "03:00",
            WinnerParticipantId = (string?)null
        };
        var stoppageResp = await client.PostAsJsonAsync(
            $"/api/data-entry/unit-results/{unitRsc}/finish-stoppage", stoppageBody);
        stoppageResp.IsSuccessStatusCode.Should().BeTrue(
            $"finish-stoppage DKO for {unitRsc} failed: {await stoppageResp.Content.ReadAsStringAsync()}");

        var confirmResp = await client.PostAsync(
            $"/api/data-entry/unit-results/{unitRsc}/confirm", content: null);
        confirmResp.IsSuccessStatusCode.Should().BeTrue(
            $"confirm DKO for {unitRsc} failed: {await confirmResp.Content.ReadAsStringAsync()}");
    }

    // ---------------------------------------------------------------
    // RSC builders
    // ---------------------------------------------------------------

    /// <summary>
    /// Builds a 34-char unit RSC: eventRsc (22) + phase (4) + unitNumber (D4) + "----".
    /// </summary>
    public static string UnitRsc(string eventRsc, string phase, int unitNumber)
        => $"{eventRsc}{phase}{unitNumber:D4}----";

    // ---------------------------------------------------------------
    // Publish helper (for idempotency tests)
    // ---------------------------------------------------------------

    public static async Task PublishEventAsync<T>(
        ProgressionWebAppFactory factory, T notification, CancellationToken ct = default)
        where T : INotification
    {
        using var scope = factory.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.Publish(notification, ct);
    }

    // ---------------------------------------------------------------
    // Seed helpers: build UnitDocument list from a unit RSC list
    // ---------------------------------------------------------------

    /// <summary>
    /// Builds UnitDocument records from the structure response.
    /// For first-round units, seeds are inferred from the bracket pairings.
    /// Later-round units have null seeds.
    /// </summary>
    public static IEnumerable<UnitDocument> BuildUnitDocuments(
        string eventRsc,
        IEnumerable<(string unitRsc, string phase, int unitNumber, int? seedA, int? seedB)> specs)
    {
        return specs.Select(s => new UnitDocument
        {
            Id = s.unitRsc,
            EventRsc = eventRsc,
            PhaseCode = s.phase,
            UnitNumber = s.unitNumber,
            SeedA = s.seedA,
            SeedB = s.seedB,
            CreatedAt = DateTime.UtcNow
        });
    }
}
