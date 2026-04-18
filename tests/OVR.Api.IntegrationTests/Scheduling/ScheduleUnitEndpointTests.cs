using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OVR.Api.IntegrationTests.Scheduling.Support;

namespace OVR.Api.IntegrationTests.Scheduling;

public class ScheduleUnitEndpointTests : IClassFixture<SchedulingWebAppFactory>
{
    private readonly HttpClient _client;

    public ScheduleUnitEndpointTests(SchedulingWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> EnsureSessionAsync(string code = "BOX10")
    {
        var body = new
        {
            code,
            venueCode = "ABC",
            name = $"Session {code}",
            startDate = "2026-04-20T10:00:00Z",
            endDate = "2026-04-20T14:00:00Z",
            leadin = "00:05:00"
        };
        await _client.PostAsJsonAsync("/api/scheduling/sessions", body);
        return code;
    }

    [Fact]
    public async Task POST_ValidPayload_Returns201()
    {
        var session = await EnsureSessionAsync("BOX10");
        var body = new
        {
            unitRsc = "BOXM57KG--------------8FNL0001----",
            locationCode = "RGA",
            startTime = "2026-04-20T10:15:00Z",
            orderInSession = 1,
            orderInLocation = 1
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/scheduling/sessions/{session}/schedule-unit", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task POST_MissingSession_Returns404()
    {
        var body = new
        {
            unitRsc = "BOXM57KG--------------8FNL0002----",
            locationCode = "RGA",
            startTime = "2026-04-20T11:00:00Z",
            orderInSession = 1,
            orderInLocation = 1
        };

        var response = await _client.PostAsJsonAsync(
            "/api/scheduling/sessions/MISSING/schedule-unit", body);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_StartTimeBeforeSession_Returns400()
    {
        var session = await EnsureSessionAsync("BOX11");
        var body = new
        {
            unitRsc = "BOXM57KG--------------8FNL0003----",
            locationCode = "RGA",
            startTime = "2026-04-20T08:00:00Z",
            orderInSession = 1,
            orderInLocation = 1
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/scheduling/sessions/{session}/schedule-unit", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("StartTimeOutsideSession");
    }

    [Fact]
    public async Task POST_AlreadyScheduled_Returns409()
    {
        var session = await EnsureSessionAsync("BOX12");
        var body = new
        {
            unitRsc = "BOXM57KG--------------8FNL0004----",
            locationCode = "RGA",
            startTime = "2026-04-20T12:00:00Z",
            orderInSession = 1,
            orderInLocation = 1
        };

        await _client.PostAsJsonAsync(
            $"/api/scheduling/sessions/{session}/schedule-unit", body);

        var second = await _client.PostAsJsonAsync(
            $"/api/scheduling/sessions/{session}/schedule-unit", body);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await second.Content.ReadAsStringAsync()).Should().Contain("UnitAlreadyScheduled");
    }

    [Fact]
    public async Task POST_CollisionAtSameLocationTime_Returns409()
    {
        var session = await EnsureSessionAsync("BOX13");
        var first = new
        {
            unitRsc = "BOXM57KG--------------8FNL0005----",
            locationCode = "RGA",
            startTime = "2026-04-20T13:00:00Z",
            orderInSession = 1,
            orderInLocation = 1
        };
        var colliding = new
        {
            unitRsc = "BOXM57KG--------------8FNL0006----",
            locationCode = "RGA",
            startTime = "2026-04-20T13:00:00Z",
            orderInSession = 2,
            orderInLocation = 2
        };

        await _client.PostAsJsonAsync(
            $"/api/scheduling/sessions/{session}/schedule-unit", first);

        var response = await _client.PostAsJsonAsync(
            $"/api/scheduling/sessions/{session}/schedule-unit", colliding);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Contain("LocationTimeOccupied");
    }
}
