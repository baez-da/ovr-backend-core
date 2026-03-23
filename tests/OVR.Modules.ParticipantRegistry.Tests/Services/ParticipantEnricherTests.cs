using FluentAssertions;
using NSubstitute;
using OVR.Modules.CommonCodes.Contracts;
using OVR.Modules.ParticipantRegistry.Domain;
using OVR.Modules.ParticipantRegistry.Services;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.ParticipantRegistry.Tests.Services;

public class ParticipantEnricherTests
{
    private readonly ICommonCodeReader _commonCodeReader = Substitute.For<ICommonCodeReader>();
    private readonly ParticipantEnricher _enricher;

    public ParticipantEnricherTests()
    {
        _enricher = new ParticipantEnricher(_commonCodeReader);
    }

    [Fact]
    public async Task EnrichCodeAsync_HappyPath_ReturnsLocalizedCode()
    {
        var text = MultilingualText.Create(new Dictionary<string, LocalizedText>
        {
            ["eng"] = LocalizedText.Create("United States of America", "USA")
        });
        _commonCodeReader.GetNameAsync(CommonCodeTypes.Organisation, "USA", Arg.Any<CancellationToken>())
            .Returns(text);

        var result = await _enricher.EnrichCodeAsync(
            CommonCodeTypes.Organisation, "USA", "eng", CancellationToken.None);

        result.Code.Should().Be("USA");
        result.Description.Long.Should().Be("United States of America");
        result.Description.Short.Should().Be("USA");
    }

    [Fact]
    public async Task EnrichCodeAsync_MissingCode_FallsBackToCodeAsDescription()
    {
        _commonCodeReader.GetNameAsync(CommonCodeTypes.Organisation, "ZZZ", Arg.Any<CancellationToken>())
            .Returns((MultilingualText?)null);

        var result = await _enricher.EnrichCodeAsync(
            CommonCodeTypes.Organisation, "ZZZ", "eng", CancellationToken.None);

        result.Code.Should().Be("ZZZ");
        result.Description.Long.Should().Be("ZZZ");
        result.Description.Short.Should().BeNull();
    }

    [Fact]
    public async Task EnrichCodeAsync_MissingLanguage_FallsBackToEnglishResolution()
    {
        var text = MultilingualText.Create(new Dictionary<string, LocalizedText>
        {
            ["eng"] = LocalizedText.Create("Swimming")
        });
        _commonCodeReader.GetNameAsync(CommonCodeTypes.Discipline, "SWM", Arg.Any<CancellationToken>())
            .Returns(text);

        // request "spa" but only "eng" exists — Resolve() falls back to "eng"
        var result = await _enricher.EnrichCodeAsync(
            CommonCodeTypes.Discipline, "SWM", "spa", CancellationToken.None);

        result.Code.Should().Be("SWM");
        result.Description.Long.Should().Be("Swimming");
        result.Description.Short.Should().BeNull();
    }

    [Fact]
    public async Task EnrichFunctionsAsync_HappyPath_ReturnsFunctionResponseList()
    {
        var fnText = MultilingualText.Create(new Dictionary<string, LocalizedText>
        {
            ["eng"] = LocalizedText.Create("Athlete")
        });
        var discText = MultilingualText.Create(new Dictionary<string, LocalizedText>
        {
            ["eng"] = LocalizedText.Create("Swimming")
        });
        _commonCodeReader.GetNameAsync(CommonCodeTypes.DisciplineFunction, "ATH", Arg.Any<CancellationToken>())
            .Returns(fnText);
        _commonCodeReader.GetNameAsync(CommonCodeTypes.Discipline, "SWM", Arg.Any<CancellationToken>())
            .Returns(discText);

        var functions = new List<ParticipantFunction>
        {
            ParticipantFunction.Create("ATH", "SWM", true)
        };

        var result = await _enricher.EnrichFunctionsAsync(functions, "eng", CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Function.Code.Should().Be("ATH");
        result[0].Function.Description.Long.Should().Be("Athlete");
        result[0].Discipline.Code.Should().Be("SWM");
        result[0].Discipline.Description.Long.Should().Be("Swimming");
        result[0].IsMain.Should().BeTrue();
    }
}
