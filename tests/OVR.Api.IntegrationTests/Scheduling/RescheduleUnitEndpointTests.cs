using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OVR.Api.IntegrationTests.Scheduling.Support;

namespace OVR.Api.IntegrationTests.Scheduling;

public class RescheduleUnitEndpointTests : IClassFixture<SchedulingWebAppFactory>
{
    private readonly HttpClient _client;

    public RescheduleUnitEndpointTests(SchedulingWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task EnsureSessionAsync(string code) =>
        await _client.PostAsJsonAsync("/api/scheduling/sessions", new
        {
            code,
            venueCode = "ABC",
            name = $"Session {code}",
            startDate = "2026-04-20T10:00:00Z",
            endDate = "2026-04-20T14:00:00Z",
            leadin = (TimeSpan?)null
        });

    private async Task<string> ScheduleUnitAsync(
        string sessionCode, string unitRsc, string locationCode, string startTime,
        int orderInSession = 1, int orderInLocation = 1)
    {
        var body = new { unitRsc, locationCode, startTime, orderInSession, orderInLocation };
        var response = await _client.PostAsJsonAsync(
            $"/api/scheduling/sessions/{sessionCode}/schedule-unit", body);
        response.EnsureSuccessStatusCode();
        return unitRsc;
    }

    [Fact]
    public async Task PATCH_ValidNewTime_Returns200()
    {
        await EnsureSessionAsync("BOX20");
        var unitRsc = await ScheduleUnitAsync(
            "BOX20", "BOXM57KG--------------8FNL0010----", "RGA", "2026-04-20T10:15:00Z");

        var body = new
        {
            sessionCode = "BOX20",
            locationCode = "RGB",
            startTime = "2026-04-20T11:00:00Z",
            orderInSession = 2,
            orderInLocation = 1,
            reason = "mat swap"
        };

        var response = await _client.PatchAsJsonAsync(
            $"/api/scheduling/unit-schedules/{unitRsc}", body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PATCH_NotFound_Returns404()
    {
        var body = new
        {
            sessionCode = "BOX20",
            locationCode = "RGA",
            startTime = "2026-04-20T11:00:00Z",
            orderInSession = 1,
            orderInLocation = 1,
            reason = (string?)null
        };

        var response = await _client.PatchAsJsonAsync(
            "/api/scheduling/unit-schedules/BOXM99KG--------------8FNL9999----", body);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PATCH_SelfCollisionIgnored_Returns200()
    {
        await EnsureSessionAsync("BOX21");
        var unitRsc = await ScheduleUnitAsync(
            "BOX21", "BOXM57KG--------------8FNL0011----", "RGA", "2026-04-20T10:30:00Z");

        // Same location and time — should NOT self-collide
        var body = new
        {
            sessionCode = "BOX21",
            locationCode = "RGA",
            startTime = "2026-04-20T10:30:00Z",
            orderInSession = 5,
            orderInLocation = 5,
            reason = (string?)null
        };

        var response = await _client.PatchAsJsonAsync(
            $"/api/scheduling/unit-schedules/{unitRsc}", body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PATCH_CollisionWithOther_Returns409()
    {
        await EnsureSessionAsync("BOX22");
        await ScheduleUnitAsync(
            "BOX22", "BOXM57KG--------------8FNL0012----", "RGA", "2026-04-20T10:45:00Z");
        var target = await ScheduleUnitAsync(
            "BOX22", "BOXM57KG--------------8FNL0013----", "RGA", "2026-04-20T11:00:00Z", 2, 2);

        var body = new
        {
            sessionCode = "BOX22",
            locationCode = "RGA",
            startTime = "2026-04-20T10:45:00Z",  // collides with first
            orderInSession = 1,
            orderInLocation = 1,
            reason = (string?)null
        };

        var response = await _client.PatchAsJsonAsync(
            $"/api/scheduling/unit-schedules/{target}", body);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
