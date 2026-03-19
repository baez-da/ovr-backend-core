using FluentAssertions;
using NSubstitute;
using OVR.Modules.CommonCodes.Features.GetCommonCodes;
using OVR.Modules.CommonCodes.Persistence;

namespace OVR.Modules.CommonCodes.Tests.Features.GetCommonCodes;

public class GetCommonCodesHandlerTests
{
    private readonly ICommonCodeRepository _repository = Substitute.For<ICommonCodeRepository>();

    [Fact]
    public async Task Handle_WithExistingType_ShouldReturnCodes()
    {
        _repository.GetByTypeAsync("SPORT", Arg.Any<CancellationToken>())
            .Returns(new List<CommonCodeDocument>
            {
                new() { Id = "SPORT:BOX", Type = "SPORT", Code = "BOX", Order = 1,
                    Name = new() { ["eng"] = new() { Long = "Boxing" } }, Attributes = [] }
            });

        var handler = new GetCommonCodesHandler(_repository);
        var result = await handler.Handle(new GetCommonCodesQuery("SPORT"), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.CommonCodes.Should().HaveCount(1);
        result.Value.CommonCodes[0].Code.Should().Be("BOX");
        result.Value.CommonCodes[0].Name.Should().ContainKey("eng");
    }

    [Fact]
    public async Task Handle_WithLanguageFilter_ShouldReturnFilteredLanguages()
    {
        _repository.GetByTypeAsync("SPORT", Arg.Any<CancellationToken>())
            .Returns(new List<CommonCodeDocument>
            {
                new() { Id = "SPORT:BOX", Type = "SPORT", Code = "BOX", Order = 1,
                    Name = new()
                    {
                        ["eng"] = new() { Long = "Boxing" },
                        ["spa"] = new() { Long = "Boxeo" },
                        ["fra"] = new() { Long = "Boxe" }
                    }, Attributes = [] }
            });

        var handler = new GetCommonCodesHandler(_repository);
        var result = await handler.Handle(
            new GetCommonCodesQuery("SPORT", ["eng", "spa"]), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.CommonCodes[0].Name.Should().HaveCount(2);
        result.Value.CommonCodes[0].Name.Should().ContainKey("eng");
        result.Value.CommonCodes[0].Name.Should().ContainKey("spa");
        result.Value.CommonCodes[0].Name.Should().NotContainKey("fra");
    }

    [Fact]
    public async Task Handle_EmptyResult_ShouldReturnEmptyList()
    {
        _repository.GetByTypeAsync("UNKNOWN", Arg.Any<CancellationToken>())
            .Returns(new List<CommonCodeDocument>());

        var handler = new GetCommonCodesHandler(_repository);
        var result = await handler.Handle(new GetCommonCodesQuery("UNKNOWN"), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.CommonCodes.Should().BeEmpty();
    }
}
