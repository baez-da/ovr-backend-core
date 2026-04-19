using System.Net.Http.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OVR.Api.IntegrationTests.DataEntry.Support;
using OVR.Modules.Scheduling.Persistence;
using OVR.SharedKernel.Domain.Events.Integration;
using Xunit;

namespace OVR.Api.IntegrationTests.DataEntry;

public class ListUnitResultsTests : IClassFixture<DataEntryWebAppFactory>
{
    private readonly DataEntryWebAppFactory _factory;

    public ListUnitResultsTests(DataEntryWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task List_BySessionCode_ReturnsOnlyUnitsInThatSession()
    {
        var eventRsc = "BOXM60KG---------";
        var rscA     = "BOXM60KG--------------FNL-0001----";
        var rscB     = "BOXM60KG--------------FNL-0002----";

        await _factory.SeedFirstRoundBracketAsync(eventRsc, rscA, 1, 2);
        await _factory.SeedFirstRoundBracketAsync(eventRsc, rscB, 3, 4);
        await _factory.SeedEntriesAsync(eventRsc, new[]
        {
            ("NOC-A-1", "ESP", 1), ("NOC-A-2", "POL", 2),
            ("NOC-A-3", "ESP", 3), ("NOC-A-4", "POL", 4)
        });

        using (var scope = _factory.Services.CreateScope())
        {
            var pub = scope.ServiceProvider.GetRequiredService<IPublisher>();
            await pub.Publish(new UnitScheduledEvent(
                rscA, eventRsc, "S-ALPHA", "BXR",
                DateTime.UtcNow, 1, 1, DateTime.UtcNow));
            await pub.Publish(new UnitScheduledEvent(
                rscB, eventRsc, "S-BETA", "BXR",
                DateTime.UtcNow, 1, 2, DateTime.UtcNow));
        }

        // Seed scheduling collection so IUnitScheduleReader can find units by session
        var schedulesCol = _factory.Database
            .GetCollection<UnitScheduleDocument>("scheduling_unit_schedules");
        await schedulesCol.InsertOneAsync(new UnitScheduleDocument
        {
            Id            = rscA,
            EventRsc      = eventRsc,
            SessionCode   = "S-ALPHA",
            LocationCode  = "BXR",
            StartTime     = DateTime.UtcNow,
            OrderInSession   = 1,
            OrderInLocation  = 1,
            Status        = "Scheduled",
            ScheduledAt   = DateTime.UtcNow
        });
        await schedulesCol.InsertOneAsync(new UnitScheduleDocument
        {
            Id            = rscB,
            EventRsc      = eventRsc,
            SessionCode   = "S-BETA",
            LocationCode  = "BXR",
            StartTime     = DateTime.UtcNow,
            OrderInSession   = 1,
            OrderInLocation  = 2,
            Status        = "Scheduled",
            ScheduledAt   = DateTime.UtcNow
        });

        var client = _factory.CreateClient();
        var list = await client.GetFromJsonAsync<List<ListItem>>(
            "/api/data-entry/unit-results?sessionCode=S-ALPHA");

        list!.Should().HaveCount(1);
        list[0].UnitRsc.Should().Be(rscA);
    }

    private record ListItem(string UnitRsc, string Status);
}
