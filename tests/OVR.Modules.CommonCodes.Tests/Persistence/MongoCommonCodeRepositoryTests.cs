using FluentAssertions;
using MongoDB.Driver;
using OVR.Modules.CommonCodes.Persistence;
using Testcontainers.MongoDb;

namespace OVR.Modules.CommonCodes.Tests.Persistence;

public class MongoCommonCodeRepositoryTests : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder()
        .WithImage("mongo:8")
        .Build();

    private MongoCommonCodeRepository _repository = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var client = new MongoClient(_container.GetConnectionString());
        var database = client.GetDatabase("ovr_test");
        _repository = new MongoCommonCodeRepository(database);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    [Fact]
    public async Task UpsertMany_ThenGetByType_ShouldReturnDocuments()
    {
        var docs = new List<CommonCodeDocument>
        {
            new() { Id = "SPORT:BOX", Type = "SPORT", Code = "BOX", Order = 1,
                Name = new() { ["eng"] = new() { Long = "Boxing" } }, Attributes = [] },
            new() { Id = "SPORT:SWM", Type = "SPORT", Code = "SWM", Order = 2,
                Name = new() { ["eng"] = new() { Long = "Swimming" } }, Attributes = [] }
        };

        await _repository.UpsertManyAsync(docs, CancellationToken.None);
        var result = await _repository.GetByTypeAsync("SPORT", CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Code.Should().Be("BOX"); // sorted by order
    }

    [Fact]
    public async Task ExistsAsync_ExistingCode_ShouldReturnTrue()
    {
        var docs = new List<CommonCodeDocument>
        {
            new() { Id = "SPORT:BOX", Type = "SPORT", Code = "BOX", Order = 1,
                Name = new() { ["eng"] = new() { Long = "Boxing" } }, Attributes = [] }
        };

        await _repository.UpsertManyAsync(docs, CancellationToken.None);

        (await _repository.ExistsAsync("SPORT", "BOX", CancellationToken.None)).Should().BeTrue();
        (await _repository.ExistsAsync("SPORT", "ZZZ", CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteByType_ShouldRemoveOnlyThatType()
    {
        var docs = new List<CommonCodeDocument>
        {
            new() { Id = "SPORT:BOX", Type = "SPORT", Code = "BOX", Order = 1,
                Name = new() { ["eng"] = new() { Long = "Boxing" } }, Attributes = [] },
            new() { Id = "COUNTRY:ARG", Type = "COUNTRY", Code = "ARG", Order = 1,
                Name = new() { ["eng"] = new() { Long = "Argentina" } }, Attributes = [] }
        };

        await _repository.UpsertManyAsync(docs, CancellationToken.None);
        await _repository.DeleteByTypeAsync("SPORT", CancellationToken.None);

        (await _repository.GetByTypeAsync("SPORT", CancellationToken.None)).Should().BeEmpty();
        (await _repository.GetByTypeAsync("COUNTRY", CancellationToken.None)).Should().HaveCount(1);
    }

    [Fact]
    public async Task UpsertMany_ExistingDocument_ShouldUpdate()
    {
        var initial = new List<CommonCodeDocument>
        {
            new() { Id = "SPORT:BOX", Type = "SPORT", Code = "BOX", Order = 1,
                Name = new() { ["eng"] = new() { Long = "Boxing" } }, Attributes = [] }
        };
        await _repository.UpsertManyAsync(initial, CancellationToken.None);

        var updated = new List<CommonCodeDocument>
        {
            new() { Id = "SPORT:BOX", Type = "SPORT", Code = "BOX", Order = 1,
                Name = new() { ["eng"] = new() { Long = "Boxing Updated" } }, Attributes = [] }
        };
        await _repository.UpsertManyAsync(updated, CancellationToken.None);

        var result = await _repository.GetAsync("SPORT", "BOX", CancellationToken.None);
        result!.Name["eng"].Long.Should().Be("Boxing Updated");
    }
}
