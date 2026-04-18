using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OVR.Api.IntegrationTests.Scheduling.Support;

namespace OVR.Api.IntegrationTests.Scheduling;

public class ListUnitsByLocationEndpointTests : IClassFixture<SchedulingWebAppFactory>
{
    private readonly HttpClient _client;

    public ListUnitsByLocationEndpointTests(SchedulingWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task SeedScheduledUnitAsync(string unitSuffix, string locationCode, string startTime)
    {
        var parsedDate = DateTimeOffset.Parse(startTime, null, System.Globalization.DateTimeStyles.RoundtripKind);
        var dateOnly = DateOnly.FromDateTime(parsedDate.Date);
        var sessionStart = new DateTimeOffset(dateOnly, new TimeOnly(8, 0), TimeSpan.Zero).ToString("o");
        var sessionEnd   = new DateTimeOffset(dateOnly, new TimeOnly(20, 0), TimeSpan.Zero).ToString("o");

        await _client.PostAsJsonAsync("/api/scheduling/sessions", new
        {
            code = $"BOX{unitSuffix}",
            venueCode = "ABC",
            name = $"Session {unitSuffix}",
            startDate = sessionStart,
            endDate = sessionEnd,
            leadin = (TimeSpan?)null
        });

        await _client.PostAsJsonAsync(
            $"/api/scheduling/sessions/BOX{unitSuffix}/schedule-unit",
            new
            {
                unitRsc = $"BOXM57KG--------------8FNL{unitSuffix}----",
                locationCode,
                startTime,
                orderInSession = 1,
                orderInLocation = 1
            });
    }

    [Fact]
    public async Task GET_WithScheduledUnits_ReturnsSortedByStartTime()
    {
        await SeedScheduledUnitAsync("0050", "RGA", "2026-04-21T14:00:00Z");
        await SeedScheduledUnitAsync("0051", "RGA", "2026-04-21T11:00:00Z");
        await SeedScheduledUnitAsync("0052", "RGA", "2026-04-21T13:00:00Z");

        var response = await _client.GetAsync(
            "/api/scheduling/locations/RGA/today?date=2026-04-21");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        var idx0051 = payload.IndexOf("8FNL0051", StringComparison.Ordinal);
        var idx0052 = payload.IndexOf("8FNL0052", StringComparison.Ordinal);
        var idx0050 = payload.IndexOf("8FNL0050", StringComparison.Ordinal);
        idx0051.Should().BeGreaterThan(0);
        idx0051.Should().BeLessThan(idx0052);
        idx0052.Should().BeLessThan(idx0050);
    }

    [Fact]
    public async Task GET_NoUnitsAtLocation_ReturnsEmpty()
    {
        var response = await _client.GetAsync(
            "/api/scheduling/locations/ZZZ/today?date=2026-04-21");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Be("[]");
    }

    [Fact]
    public async Task GET_FiltersToRequestedDate_IgnoresOtherDays()
    {
        await SeedScheduledUnitAsync("0060", "RGC", "2026-04-22T10:00:00Z");
        await SeedScheduledUnitAsync("0061", "RGC", "2026-04-23T10:00:00Z");

        var response = await _client.GetAsync(
            "/api/scheduling/locations/RGC/today?date=2026-04-22");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain("8FNL0060");
        payload.Should().NotContain("8FNL0061");
    }
}
