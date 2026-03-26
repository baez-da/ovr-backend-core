using FluentAssertions;
using NSubstitute;
using OVR.Modules.CommonCodes.Features.BulkGetCommonCodes;
using OVR.SharedKernel.Contracts;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.CommonCodes.Tests.Features.BulkGetCommonCodes;

public class BulkGetCommonCodesEndpointTests
{
    private readonly ICommonCodeCache _cache = Substitute.For<ICommonCodeCache>();

    private void SetupCache()
    {
        var countryEntries = new Dictionary<string, CommonCodeEntry>
        {
            ["POL"] = new("POL", 1,
                new Dictionary<string, LocalizedText>
                {
                    ["eng"] = LocalizedText.Create("Poland", "POL"),
                    ["spa"] = LocalizedText.Create("Polonia", "POL")
                },
                new Dictionary<string, string> { ["continent"] = "EUR" }),
        };
        _cache.GetByType("COUNTRY").Returns(countryEntries);
        _cache.GetAvailableTypes().Returns(new List<string> { "COUNTRY", "DISCIPLINE" });
        _cache.GetVersion("COUNTRY").Returns("abc123");
        _cache.GetVersion("DISCIPLINE").Returns("def456");
    }

    [Fact]
    public void BuildBulkResponse_WithTypeFilter_ReturnsOnlyRequestedTypes()
    {
        SetupCache();

        var response = BulkGetCommonCodesHandler.BuildResponse(_cache, ["COUNTRY"], null);

        response.Types.Should().ContainKey("COUNTRY");
        response.Types.Should().NotContainKey("DISCIPLINE");
    }

    [Fact]
    public void BuildBulkResponse_WithoutTypeFilter_ReturnsAllTypes()
    {
        SetupCache();

        var response = BulkGetCommonCodesHandler.BuildResponse(_cache, null, null);

        response.Types.Should().ContainKey("COUNTRY");
        response.Types.Should().ContainKey("DISCIPLINE");
    }

    [Fact]
    public void BuildBulkResponse_WithLanguageFilter_FiltersTranslations()
    {
        SetupCache();

        var response = BulkGetCommonCodesHandler.BuildResponse(_cache, ["COUNTRY"], ["eng"]);

        var polName = response.Types["COUNTRY"].Codes["POL"].Name;
        polName.Should().ContainKey("eng");
        polName.Should().NotContainKey("spa");
    }

    [Fact]
    public void BuildBulkResponse_HasVersionPerTypeAndGlobal()
    {
        SetupCache();

        var response = BulkGetCommonCodesHandler.BuildResponse(_cache, ["COUNTRY"], null);

        response.Version.Should().NotBeNullOrEmpty();
        response.Types["COUNTRY"].Version.Should().Be("abc123");
    }
}
