using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OVR.Api.IntegrationTests.Scheduling.Support;

namespace OVR.Api.IntegrationTests.Scheduling;

public class UnscheduleUnitEndpointTests : IClassFixture<SchedulingWebAppFactory>
{
    private readonly HttpClient _client;

    public UnscheduleUnitEndpointTests(SchedulingWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DELETE_Existing_Returns204AndPersistenceIsGone()
    {
        await _client.PostAsJsonAsync("/api/scheduling/sessions", new
        {
            code = "BOX30",
            venueCode = "ABC",
            name = "unschedule test",
            startDate = "2026-04-20T10:00:00Z",
            endDate = "2026-04-20T14:00:00Z",
            leadin = (TimeSpan?)null
        });

        await _client.PostAsJsonAsync(
            "/api/scheduling/sessions/BOX30/schedule-unit",
            new
            {
                unitRsc = "BOXM57KG--------------8FNL0020----",
                locationCode = "RGA",
                startTime = "2026-04-20T10:30:00Z",
                orderInSession = 1,
                orderInLocation = 1
            });

        var response = await _client.DeleteAsync(
            "/api/scheduling/unit-schedules/BOXM57KG--------------8FNL0020----");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var reSchedule = await _client.PostAsJsonAsync(
            "/api/scheduling/sessions/BOX30/schedule-unit",
            new
            {
                unitRsc = "BOXM57KG--------------8FNL0020----",
                locationCode = "RGB",
                startTime = "2026-04-20T11:00:00Z",
                orderInSession = 2,
                orderInLocation = 1
            });
        reSchedule.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task DELETE_NotFound_Returns404()
    {
        var response = await _client.DeleteAsync(
            "/api/scheduling/unit-schedules/BOXM99KG--------------8FNL9999----");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
