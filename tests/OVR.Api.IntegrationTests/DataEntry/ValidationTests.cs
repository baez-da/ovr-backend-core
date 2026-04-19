using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OVR.Api.IntegrationTests.DataEntry.Support;
using OVR.SharedKernel.Domain.Events.Integration;
using Xunit;

namespace OVR.Api.IntegrationTests.DataEntry;

public class ValidationTests : IClassFixture<DataEntryWebAppFactory>
{
    private readonly DataEntryWebAppFactory _factory;

    public ValidationTests(DataEntryWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task GetUnitResult_NotFound_Returns404()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync(
            "/api/data-entry/unit-results/DOES-NOT-EXIST-------------------");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Start_WhenAlreadyLive_Returns400()
    {
        var eventRsc = "BOXM91KG---------";
        var unitRsc  = "BOXM91KG--------------FNL-0001----";
        await _factory.SeedFirstRoundBracketAsync(eventRsc, unitRsc, 1, 2);
        await _factory.SeedEntriesAsync(eventRsc, new[]
        {
            ("NOC-A-0001", "ESP", 1), ("NOC-B-0001", "POL", 2)
        });
        using (var scope = _factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IPublisher>()
                .Publish(new UnitScheduledEvent(
                    unitRsc, eventRsc, "S1", "BXR",
                    DateTime.UtcNow, 1, 1, DateTime.UtcNow));
        }

        var client = _factory.CreateClient();
        await client.PostAsync($"/api/data-entry/unit-results/{unitRsc}/start", null);
        var again = await client.PostAsync($"/api/data-entry/unit-results/{unitRsc}/start", null);
        again.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ScorePeriod_ScoreOutOfRange_Returns400()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync(
            "/api/data-entry/unit-results/ANY/periods/R1/score",
            new { Scorecards = new[] { new { JudgePos = "J1", HomeScore = 11, AwayScore = 9 } } });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task FinishByStoppage_Wp_Returns400()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync(
            "/api/data-entry/unit-results/ANY/finish-stoppage",
            new { ResultCode = "Wp", StoppageRound = "R2", StoppageTime = "01:00" });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
