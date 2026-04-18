using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OVR.Api.IntegrationTests.Scheduling.Support;

namespace OVR.Api.IntegrationTests.Scheduling;

public class CreateSessionEndpointTests : IClassFixture<SchedulingWebAppFactory>
{
    private readonly HttpClient _client;

    public CreateSessionEndpointTests(SchedulingWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task POST_ValidPayload_Returns201()
    {
        var body = new
        {
            code = "BOX01",
            venueCode = "ABC",
            name = "Boxing Session 1",
            startDate = "2026-04-20T10:00:00Z",
            endDate = "2026-04-20T14:00:00Z",
            leadin = "00:05:00"
        };

        var response = await _client.PostAsJsonAsync("/api/scheduling/sessions", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.ToString().Should().Contain("BOX01");
    }

    [Fact]
    public async Task POST_UnknownVenue_Returns400()
    {
        var body = new
        {
            code = "BOX02",
            venueCode = "ZZZ",
            name = "x",
            startDate = "2026-04-20T10:00:00Z",
            endDate = "2026-04-20T14:00:00Z",
            leadin = (TimeSpan?)null
        };

        var response = await _client.PostAsJsonAsync("/api/scheduling/sessions", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain("Scheduling.InvalidVenue");
    }

    [Fact]
    public async Task POST_DuplicateCode_Returns409()
    {
        var body = new
        {
            code = "BOX03",
            venueCode = "ABC",
            name = "duplicate test",
            startDate = "2026-04-20T10:00:00Z",
            endDate = "2026-04-20T14:00:00Z",
            leadin = (TimeSpan?)null
        };

        await _client.PostAsJsonAsync("/api/scheduling/sessions", body);
        var second = await _client.PostAsJsonAsync("/api/scheduling/sessions", body);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task POST_EndBeforeStart_Returns400FromValidator()
    {
        var body = new
        {
            code = "BOX04",
            venueCode = "ABC",
            name = "bad dates",
            startDate = "2026-04-20T14:00:00Z",
            endDate = "2026-04-20T10:00:00Z",
            leadin = (TimeSpan?)null
        };

        var response = await _client.PostAsJsonAsync("/api/scheduling/sessions", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
