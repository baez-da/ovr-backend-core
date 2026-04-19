using System.Net.Http.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OVR.Api.IntegrationTests.DataEntry.Support;
using OVR.SharedKernel.Domain.Events.Integration;
using Xunit;

namespace OVR.Api.IntegrationTests.DataEntry;

public class CreateUnitResultOnScheduledTests : IClassFixture<DataEntryWebAppFactory>
{
    private readonly DataEntryWebAppFactory _factory;

    public CreateUnitResultOnScheduledTests(DataEntryWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UnitScheduledEvent_CreatesUnitResultWithCorrectLineup()
    {
        var eventRsc = "BOXM57KG---------";
        var unitRsc  = "BOXM57KG--------------FNL-0001----";

        await _factory.SeedFirstRoundBracketAsync(eventRsc, unitRsc, seedA: 1, seedB: 2);
        await _factory.SeedEntriesAsync(eventRsc, new[]
        {
            ("NOC-ESP-0001", "ESP", 1),
            ("NOC-POL-0014", "POL", 2)
        });

        using var scope = _factory.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.Publish(new UnitScheduledEvent(
            UnitRsc: unitRsc, EventRsc: eventRsc,
            SessionCode: "S1", LocationCode: "BXR",
            StartTime: DateTime.UtcNow, OrderInSession: 1, OrderInLocation: 1,
            ScheduledAt: DateTime.UtcNow));

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/data-entry/unit-results/{unitRsc}");
        response.IsSuccessStatusCode.Should().BeTrue();

        var body = await response.Content.ReadFromJsonAsync<UnitResultDto>();
        body!.Status.Should().Be("StartList");
        body.Competitors.Should().HaveCount(2);
        body.Competitors[0].SortOrder.Should().Be(1);
        body.Competitors[0].Seed.Should().Be(1);
        body.Competitors[0].Organisation.Should().Be("ESP");
        body.Competitors[1].SortOrder.Should().Be(2);
        body.Competitors[1].Organisation.Should().Be("POL");
    }

    [Fact]
    public async Task UnitScheduledEvent_Idempotent_DoesNotCreateDuplicate()
    {
        var eventRsc = "BOXM66KG---------";
        var unitRsc  = "BOXM66KG--------------FNL-0001----";

        await _factory.SeedFirstRoundBracketAsync(eventRsc, unitRsc, 1, 2);
        await _factory.SeedEntriesAsync(eventRsc, new[]
        {
            ("NOC-ESP-0010", "ESP", 1),
            ("NOC-POL-0011", "POL", 2)
        });

        using var scope = _factory.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        var evt = new UnitScheduledEvent(
            unitRsc, eventRsc, "S1", "BXR",
            DateTime.UtcNow, 1, 1, DateTime.UtcNow);

        await publisher.Publish(evt);
        await publisher.Publish(evt);  // second time — should no-op

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/data-entry/unit-results/{unitRsc}");
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    private record UnitResultDto(
        string UnitRsc, string Status, string? CurrentPeriodCode,
        DateTime? StartedAt, DateTime? EndedAt,
        List<CompetitorDto> Competitors);

    private record CompetitorDto(
        int SortOrder, string? ParticipantId, int? Seed,
        string Organisation, string? Wlt);
}
