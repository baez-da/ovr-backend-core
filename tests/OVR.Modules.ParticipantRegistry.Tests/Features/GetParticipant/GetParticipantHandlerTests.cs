using FluentAssertions;
using NSubstitute;
using OVR.Modules.CommonCodes.Contracts;
using OVR.Modules.ParticipantRegistry.Domain;
using OVR.Modules.ParticipantRegistry.Features.GetParticipant;
using OVR.Modules.ParticipantRegistry.Persistence;
using OVR.Modules.ParticipantRegistry.Services;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.ParticipantRegistry.Tests.Features.GetParticipant;

public class GetParticipantHandlerTests
{
    private readonly IParticipantRepository _repository = Substitute.For<IParticipantRepository>();
    private readonly ICommonCodeReader _commonCodeReader = Substitute.For<ICommonCodeReader>();
    private readonly GetParticipantHandler _handler;

    public GetParticipantHandlerTests()
    {
        var enricher = new ParticipantEnricher(_commonCodeReader);
        _handler = new GetParticipantHandler(_repository, enricher);
    }

    private static Participant CreateTestParticipant(string id = "LOC-001")
    {
        var participantId = ParticipantId.Create(id);
        var biographicData = BiographicData.Create("John", "Smith", Gender.FromCode("M"), null, Organisation.Create("USA"));
        var functions = new List<ParticipantFunction> { ParticipantFunction.Create("ATH", "SWM", true) };
        return Participant.Hydrate(
            participantId, biographicData, null, functions,
            "SMITH John", "SMITH J", "John SMITH", "J. SMITH",
            "SMITH", "SMITH John", "SMITH J", "SMITH John",
            DateTime.UtcNow, null, "https://example.com/photo.jpg");
    }

    [Fact]
    public async Task Handle_ExistingParticipant_ReturnsEnrichedResponse()
    {
        var participant = CreateTestParticipant();
        _repository.GetByIdAsync("LOC-001", Arg.Any<CancellationToken>()).Returns(participant);

        var orgText = MultilingualText.Create(new Dictionary<string, LocalizedText>
        {
            ["eng"] = LocalizedText.Create("United States of America", "USA")
        });
        var genderText = MultilingualText.Create(new Dictionary<string, LocalizedText>
        {
            ["eng"] = LocalizedText.Create("Male", "M")
        });
        var fnText = MultilingualText.Create(new Dictionary<string, LocalizedText>
        {
            ["eng"] = LocalizedText.Create("Athlete")
        });
        var discText = MultilingualText.Create(new Dictionary<string, LocalizedText>
        {
            ["eng"] = LocalizedText.Create("Swimming")
        });

        _commonCodeReader.GetNameAsync(CommonCodeTypes.Organisation, "USA", Arg.Any<CancellationToken>())
            .Returns(orgText);
        _commonCodeReader.GetNameAsync(CommonCodeTypes.PersonGender, "M", Arg.Any<CancellationToken>())
            .Returns(genderText);
        _commonCodeReader.GetNameAsync(CommonCodeTypes.DisciplineFunction, "ATH", Arg.Any<CancellationToken>())
            .Returns(fnText);
        _commonCodeReader.GetNameAsync(CommonCodeTypes.Discipline, "SWM", Arg.Any<CancellationToken>())
            .Returns(discText);

        var query = new GetParticipantQuery("LOC-001", "eng");
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Id.Should().Be("LOC-001");
        result.Value.Organisation.Code.Should().Be("USA");
        result.Value.Organisation.Description.Long.Should().Be("United States of America");
        result.Value.Gender.Code.Should().Be("M");
        result.Value.Gender.Description.Long.Should().Be("Male");
        result.Value.Functions.Should().HaveCount(1);
        result.Value.Functions[0].Function.Code.Should().Be("ATH");
        result.Value.Functions[0].Discipline.Code.Should().Be("SWM");
        result.Value.PhotoUrl.Should().Be("https://example.com/photo.jpg");
    }

    [Fact]
    public async Task Handle_NonExistentParticipant_ReturnsNotFoundError()
    {
        _repository.GetByIdAsync("DOES-NOT-EXIST", Arg.Any<CancellationToken>()).Returns((Participant?)null);

        var query = new GetParticipantQuery("DOES-NOT-EXIST", "eng");
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Participant.NotFound");
    }
}
