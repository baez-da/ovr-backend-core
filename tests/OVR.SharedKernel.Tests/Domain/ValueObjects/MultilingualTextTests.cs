using FluentAssertions;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.SharedKernel.Tests.Domain.ValueObjects;

public class MultilingualTextTests
{
    [Fact]
    public void Create_WithTranslations_ShouldSucceed()
    {
        var translations = new Dictionary<string, LocalizedText>
        {
            ["eng"] = LocalizedText.Create("Boxing", "BOX"),
            ["spa"] = LocalizedText.Create("Boxeo", "BOX")
        };

        var text = MultilingualText.Create(translations);

        text.All.Should().HaveCount(2);
    }

    [Fact]
    public void Create_WithEmptyDictionary_ShouldThrow()
    {
        var act = () => MultilingualText.Create(new Dictionary<string, LocalizedText>());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldNormalizeKeysToLowercase()
    {
        var translations = new Dictionary<string, LocalizedText>
        {
            ["ENG"] = LocalizedText.Create("Boxing")
        };

        var text = MultilingualText.Create(translations);

        text.GetOrDefault("eng").Should().NotBeNull();
        text.GetOrDefault("ENG").Should().NotBeNull();
    }

    [Fact]
    public void GetOrDefault_ExistingLanguage_ShouldReturnText()
    {
        var text = CreateSample();

        var result = text.GetOrDefault("eng");

        result.Should().NotBeNull();
        result!.Long.Should().Be("Boxing");
    }

    [Fact]
    public void GetOrDefault_MissingLanguage_ShouldReturnNull()
    {
        var text = CreateSample();

        text.GetOrDefault("por").Should().BeNull();
    }

    [Fact]
    public void Resolve_ExistingLanguage_ShouldReturnIt()
    {
        var text = CreateSample();

        var result = text.Resolve("spa");

        result.Long.Should().Be("Boxeo");
    }

    [Fact]
    public void Resolve_MissingLanguage_ShouldFallbackToEng()
    {
        var text = CreateSample();

        var result = text.Resolve("por");

        result.Long.Should().Be("Boxing");
    }

    [Fact]
    public void Resolve_MissingLanguageAndFallback_ShouldReturnFirst()
    {
        var translations = new Dictionary<string, LocalizedText>
        {
            ["fra"] = LocalizedText.Create("Boxe")
        };
        var text = MultilingualText.Create(translations);

        var result = text.Resolve("por");

        result.Long.Should().Be("Boxe");
    }

    [Fact]
    public void Filter_ExistingLanguages_ShouldReturnSubset()
    {
        var text = CreateSample();

        var filtered = text.Filter(["spa"]);

        filtered.All.Should().HaveCount(1);
        filtered.All.Should().ContainKey("spa");
    }

    [Fact]
    public void Filter_NoMatchingLanguages_ShouldReturnOriginal()
    {
        var text = CreateSample();

        var filtered = text.Filter(["por"]);

        filtered.All.Should().HaveCount(2);
    }

    [Fact]
    public void Equality_SameTranslations_ShouldBeEqual()
    {
        var a = CreateSample();
        var b = CreateSample();

        a.Should().Be(b);
    }

    private static MultilingualText CreateSample() =>
        MultilingualText.Create(new Dictionary<string, LocalizedText>
        {
            ["eng"] = LocalizedText.Create("Boxing", "BOX"),
            ["spa"] = LocalizedText.Create("Boxeo", "BOX")
        });
}
