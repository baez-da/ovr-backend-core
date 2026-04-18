using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OVR.Api.IntegrationTests.CompetitionConfig.Support;

namespace OVR.Api.IntegrationTests.CompetitionConfig;

public class GenerateEventStructureEndpointTests : IClassFixture<CompetitionConfigWebAppFactory>
{
    private readonly HttpClient _client;

    public GenerateEventStructureEndpointTests(CompetitionConfigWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> CreateBoxEventAsync(string eventCode)
    {
        var body = new { discipline = "BOX", gender = "M", eventCode, modifier = (string?)null, name = $"Men's {eventCode}" };
        var response = await _client.PostAsJsonAsync("/api/competition-config/events", body);
        response.EnsureSuccessStatusCode();
        var location = response.Headers.Location!.OriginalString;
        return location.Split('/').Last();
    }

    [Fact]
    public async Task POST_Size16_Returns200And15Units()
    {
        var rsc = await CreateBoxEventAsync("57KG");
        var body = new { format = "SingleElimination", size = 16, startUnitNumber = 1 };

        var response = await _client.PostAsJsonAsync(
            $"/api/competition-config/events/{rsc}/generate-structure", body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain("\"size\":16");
        payload.Should().Contain("8FNL0001----");
        payload.Should().Contain("FNL-0015----");
    }

    [Fact]
    public async Task POST_Size13_Returns200And15Units()
    {
        var rsc = await CreateBoxEventAsync("60KG");
        var body = new { format = "SingleElimination", size = 13, startUnitNumber = 1 };

        var response = await _client.PostAsJsonAsync(
            $"/api/competition-config/events/{rsc}/generate-structure", body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain("\"size\":13");
    }

    [Fact]
    public async Task POST_AlreadyGenerated_Returns409()
    {
        var rsc = await CreateBoxEventAsync("63KG");
        var body = new { format = "SingleElimination", size = 8, startUnitNumber = 1 };
        await _client.PostAsJsonAsync($"/api/competition-config/events/{rsc}/generate-structure", body);

        var second = await _client.PostAsJsonAsync(
            $"/api/competition-config/events/{rsc}/generate-structure", body);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task POST_MissingEvent_Returns404()
    {
        var body = new { format = "SingleElimination", size = 8, startUnitNumber = 1 };
        var fakeRsc = "BOXM99KG--------------------------";

        var response = await _client.PostAsJsonAsync(
            $"/api/competition-config/events/{fakeRsc}/generate-structure", body);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_Size1_Returns400FromValidator()
    {
        var rsc = await CreateBoxEventAsync("66KG");
        var body = new { format = "SingleElimination", size = 1, startUnitNumber = 1 };

        var response = await _client.PostAsJsonAsync(
            $"/api/competition-config/events/{rsc}/generate-structure", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
