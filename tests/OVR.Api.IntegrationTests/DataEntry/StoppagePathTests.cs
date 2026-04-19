using System.Net.Http.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OVR.Api.IntegrationTests.DataEntry.Support;
using OVR.SharedKernel.Domain.Events.Integration;
using Xunit;

namespace OVR.Api.IntegrationTests.DataEntry;

public class StoppagePathTests : IClassFixture<DataEntryWebAppFactory>
{
    private readonly DataEntryWebAppFactory _factory;

    public StoppagePathTests(DataEntryWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task TkoI_InRound2_SetsRmPointsDecisionAndOfficializes()
    {
        var eventRsc = "BOXM81KG---------";
        var unitRsc  = "BOXM81KG--------------FNL-0001----";
        await _factory.SeedFirstRoundBracketAsync(eventRsc, unitRsc, 1, 2);
        await _factory.SeedEntriesAsync(eventRsc, new[]
        {
            ("NOC-ESP-0030", "ESP", 1),
            ("NOC-POL-0031", "POL", 2)
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
        await client.PostAsJsonAsync(
            $"/api/data-entry/unit-results/{unitRsc}/periods/R1/score",
            new ScorePeriodBody(new[]
            {
                new ScorecardBody("J1", 10, 9),
                new ScorecardBody("J2", 10, 9),
                new ScorecardBody("J3", 10, 9)
            }));

        var finish = await client.PostAsJsonAsync(
            $"/api/data-entry/unit-results/{unitRsc}/finish-stoppage",
            new FinishStoppageBody("TkoI", "R2", "01:30", "NOC-ESP-0030"));
        finish.IsSuccessStatusCode.Should().BeTrue();

        await client.PostAsync($"/api/data-entry/unit-results/{unitRsc}/confirm", null);

        var final = await client.GetFromJsonAsync<UnitResultDto>(
            $"/api/data-entry/unit-results/{unitRsc}");
        final!.Status.Should().Be("Official");
        final.Decision!.Code.Should().Be("TkoI");
        final.Decision.Type.Should().Be("RmPoints");
        final.Decision.StoppageRound.Should().Be("R2");
        final.Decision.StoppageTime.Should().Be("01:30");
    }

    private record ScorePeriodBody(ScorecardBody[] Scorecards);
    private record ScorecardBody(string JudgePos, int HomeScore, int AwayScore);
    private record FinishStoppageBody(
        string ResultCode, string StoppageRound, string StoppageTime,
        string? WinnerParticipantId);
    private record UnitResultDto(string Status, DecisionDto? Decision);
    private record DecisionDto(
        string Type, string Code, string? StoppageRound, string? StoppageTime);
}
