using System.Net.Http.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OVR.Api.IntegrationTests.DataEntry.Support;
using OVR.SharedKernel.Domain.Events.Integration;
using Xunit;

namespace OVR.Api.IntegrationTests.DataEntry;

public class ScoringPathTests : IClassFixture<DataEntryWebAppFactory>
{
    private readonly DataEntryWebAppFactory _factory;

    public ScoringPathTests(DataEntryWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task FullPointsPath_StartList_Through_Official()
    {
        var eventRsc = "BOXM75KG---------";
        var unitRsc  = "BOXM75KG--------------FNL-0001----";
        await _factory.SeedFirstRoundBracketAsync(eventRsc, unitRsc, 1, 2);
        await _factory.SeedEntriesAsync(eventRsc, new[]
        {
            ("NOC-ESP-0020", "ESP", 1),
            ("NOC-POL-0021", "POL", 2)
        });

        using (var scope = _factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IPublisher>()
                .Publish(new UnitScheduledEvent(
                    unitRsc, eventRsc, "S1", "BXR",
                    DateTime.UtcNow, 1, 1, DateTime.UtcNow));
        }

        var client = _factory.CreateClient();

        var startResp = await client.PostAsync(
            $"/api/data-entry/unit-results/{unitRsc}/start", content: null);
        startResp.IsSuccessStatusCode.Should().BeTrue();

        var unanimousRed = new ScorePeriodBody(new[]
        {
            new ScorecardBody("J1", 10, 9),
            new ScorecardBody("J2", 10, 9),
            new ScorecardBody("J3", 10, 9)
        });
        foreach (var code in new[] { "R1", "R2", "R3" })
        {
            var resp = await client.PostAsJsonAsync(
                $"/api/data-entry/unit-results/{unitRsc}/periods/{code}/score", unanimousRed);
            resp.IsSuccessStatusCode.Should().BeTrue();
        }

        var confirmResp = await client.PostAsync(
            $"/api/data-entry/unit-results/{unitRsc}/confirm", content: null);
        confirmResp.IsSuccessStatusCode.Should().BeTrue();

        var final = await client.GetFromJsonAsync<UnitResultDto>(
            $"/api/data-entry/unit-results/{unitRsc}");
        final!.Status.Should().Be("Official");
        final.Decision!.Code.Should().Be("Wp");
        final.Decision.DecisionMark.Should().Be("3:0");
        final.Decision.WinnerParticipantId.Should().Be("NOC-ESP-0020");
        final.Competitors[0].Wlt.Should().Be("W");
        final.Competitors[1].Wlt.Should().Be("L");
    }

    private record ScorePeriodBody(ScorecardBody[] Scorecards);
    private record ScorecardBody(string JudgePos, int HomeScore, int AwayScore);
    private record UnitResultDto(
        string UnitRsc, string Status, DecisionDto? Decision,
        List<CompetitorDto> Competitors);
    private record DecisionDto(
        string Type, string Code, string? DecisionMark, string? WinnerParticipantId);
    private record CompetitorDto(int SortOrder, string ParticipantId, string? Wlt);
}
