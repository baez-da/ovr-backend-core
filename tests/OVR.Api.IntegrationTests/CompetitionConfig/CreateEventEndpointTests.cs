using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OVR.Api.IntegrationTests.CompetitionConfig.Support;

namespace OVR.Api.IntegrationTests.CompetitionConfig;

public class CreateEventEndpointTests : IClassFixture<CompetitionConfigWebAppFactory>
{
    private readonly HttpClient _client;

    public CreateEventEndpointTests(CompetitionConfigWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task POST_ValidPayload_Returns201()
    {
        var body = new
        {
            discipline = "BOX",
            gender = "M",
            eventCode = "57KG",
            modifier = (string?)null,
            name = "Men's 57kg"
        };

        var response = await _client.PostAsJsonAsync("/api/competition-config/events", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.ToString()
            .Should().Contain("BOXM57KG");
    }

    [Fact]
    public async Task POST_UnknownDiscipline_Returns400()
    {
        var body = new
        {
            discipline = "ZZZ",
            gender = "M",
            eventCode = "57KG",
            modifier = (string?)null,
            name = "Men's 57kg"
        };

        var response = await _client.PostAsJsonAsync("/api/competition-config/events", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body2 = await response.Content.ReadAsStringAsync();
        body2.Should().Contain("CompetitionConfig.InvalidDiscipline");
    }

    [Fact]
    public async Task POST_DuplicateRsc_Returns409()
    {
        var body = new
        {
            discipline = "BOX",
            gender = "M",
            eventCode = "60KG",
            modifier = (string?)null,
            name = "Men's 60kg duplicate test"
        };

        await _client.PostAsJsonAsync("/api/competition-config/events", body);
        var second = await _client.PostAsJsonAsync("/api/competition-config/events", body);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task POST_MissingGender_Returns400FromValidator()
    {
        var body = new
        {
            discipline = "BOX",
            gender = "",
            eventCode = "63KG",
            modifier = (string?)null,
            name = "x"
        };

        var response = await _client.PostAsJsonAsync("/api/competition-config/events", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
