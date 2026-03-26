using FluentAssertions;
using NSubstitute;
using OVR.Modules.CommonCodes.Persistence;
using OVR.Modules.CommonCodes.Services;
using OVR.SharedKernel.Domain.Events.Integration;

namespace OVR.Modules.CommonCodes.Tests.Services;

public class CommonCodeCacheServiceTests
{
    private readonly ICommonCodeRepository _repository = Substitute.For<ICommonCodeRepository>();
    private readonly CommonCodeCacheService _cache;

    public CommonCodeCacheServiceTests()
    {
        _cache = new CommonCodeCacheService(_repository);
    }

    private static List<CommonCodeDocument> CreateCountryDocs() =>
    [
        new()
        {
            Id = "COUNTRY:POL", Type = "COUNTRY", Code = "POL", Order = 1,
            Name = new Dictionary<string, LocalizedTextDocument>
            {
                ["eng"] = new() { Long = "Poland", Short = "POL" },
                ["spa"] = new() { Long = "Polonia", Short = "POL" }
            },
            Attributes = new Dictionary<string, string> { ["continent"] = "EUR" }
        },
        new()
        {
            Id = "COUNTRY:USA", Type = "COUNTRY", Code = "USA", Order = 2,
            Name = new Dictionary<string, LocalizedTextDocument>
            {
                ["eng"] = new() { Long = "United States of America", Short = "USA" }
            },
            Attributes = []
        }
    ];

    private async Task HydrateCacheAsync()
    {
        _repository.GetDistinctTypesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<string> { "COUNTRY" });
        _repository.GetByTypeAsync("COUNTRY", Arg.Any<CancellationToken>())
            .Returns(CreateCountryDocs());
        await _cache.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_LoadsAllTypesFromRepository()
    {
        await HydrateCacheAsync();

        _cache.GetAvailableTypes().Should().Contain("COUNTRY");
        _cache.Exists("COUNTRY", "POL").Should().BeTrue();
    }

    [Fact]
    public async Task Exists_UnknownCode_ReturnsFalse()
    {
        await HydrateCacheAsync();

        _cache.Exists("COUNTRY", "ZZZ").Should().BeFalse();
        _cache.Exists("UNKNOWN_TYPE", "POL").Should().BeFalse();
    }

    [Fact]
    public async Task GetDescription_ReturnsLongDescriptionForLanguage()
    {
        await HydrateCacheAsync();

        _cache.GetDescription("COUNTRY", "POL", "eng").Should().Be("Poland");
        _cache.GetDescription("COUNTRY", "POL", "spa").Should().Be("Polonia");
    }

    [Fact]
    public async Task GetDescription_UnknownLanguage_ReturnsNull()
    {
        await HydrateCacheAsync();

        _cache.GetDescription("COUNTRY", "POL", "fra").Should().BeNull();
    }

    [Fact]
    public async Task GetDescription_UnknownCode_ReturnsNull()
    {
        await HydrateCacheAsync();

        _cache.GetDescription("COUNTRY", "ZZZ", "eng").Should().BeNull();
    }

    [Fact]
    public async Task GetByType_ReturnsDictionaryOfEntries()
    {
        await HydrateCacheAsync();

        var result = _cache.GetByType("COUNTRY");
        result.Should().ContainKey("POL");
        result.Should().ContainKey("USA");
        result["POL"].Order.Should().Be(1);
    }

    [Fact]
    public async Task GetByType_UnknownType_ReturnsEmptyDictionary()
    {
        await HydrateCacheAsync();

        _cache.GetByType("UNKNOWN").Should().BeEmpty();
    }

    [Fact]
    public async Task GetVersion_ReturnsDeterministicHash()
    {
        await HydrateCacheAsync();

        var v1 = _cache.GetVersion("COUNTRY");
        var v2 = _cache.GetVersion("COUNTRY");
        v1.Should().NotBeNullOrEmpty();
        v1.Should().Be(v2);
    }

    [Fact]
    public async Task GetVersion_UnknownType_ReturnsEmptyString()
    {
        await HydrateCacheAsync();

        _cache.GetVersion("UNKNOWN").Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReimportEvent_ReloadsAffectedType()
    {
        await HydrateCacheAsync();
        var versionBefore = _cache.GetVersion("COUNTRY");

        var updatedDocs = new List<CommonCodeDocument>
        {
            new()
            {
                Id = "COUNTRY:POL", Type = "COUNTRY", Code = "POL", Order = 1,
                Name = new Dictionary<string, LocalizedTextDocument>
                {
                    ["eng"] = new() { Long = "Republic of Poland", Short = "POL" }
                },
                Attributes = []
            }
        };
        _repository.GetByTypeAsync("COUNTRY", Arg.Any<CancellationToken>())
            .Returns(updatedDocs);

        await _cache.Handle(
            new CommonCodesReimportedEvent("COUNTRY", "overwrite", ["POL"]),
            CancellationToken.None);

        _cache.GetDescription("COUNTRY", "POL", "eng").Should().Be("Republic of Poland");
        _cache.Exists("COUNTRY", "USA").Should().BeFalse();
        _cache.GetVersion("COUNTRY").Should().NotBe(versionBefore);
    }
}
