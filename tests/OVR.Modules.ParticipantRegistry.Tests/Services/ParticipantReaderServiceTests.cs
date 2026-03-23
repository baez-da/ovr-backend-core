using FluentAssertions;
using NSubstitute;
using OVR.Modules.CommonCodes.Contracts;
using OVR.Modules.ParticipantRegistry.Domain;
using OVR.Modules.ParticipantRegistry.Persistence;
using OVR.Modules.ParticipantRegistry.Services;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.ParticipantRegistry.Tests.Services;

public class ParticipantReaderServiceTests
{
    private readonly IParticipantRepository _repository = Substitute.For<IParticipantRepository>();
    private readonly ICommonCodeReader _commonCodeReader = Substitute.For<ICommonCodeReader>();
    private readonly ParticipantReaderService _service;

    public ParticipantReaderServiceTests()
    {
        var enricher = new ParticipantEnricher(_commonCodeReader);
        _service = new ParticipantReaderService(_repository, enricher);
    }

    private static Participant CreateTestParticipant(string id = "LOC-001")
    {
        var participantId = ParticipantId.Create(id);
        var description = Description.Create("John", "Smith", Gender.FromCode("M"), null, Organisation.Create("USA"));
        var functions = new List<ParticipantFunction> { ParticipantFunction.Create("ATH", "SWM", true) };
        return Participant.Hydrate(
            participantId, description, null, functions,
            "SMITH John", "SMITH J", "John SMITH", "J. SMITH",
            "SMITH", "SMITH John", "SMITH J", "SMITH John",
            DateTime.UtcNow, null, "https://example.com/photo.jpg");
    }

    private void SetupDefaultCommonCodes()
    {
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
    }

    [Fact]
    public async Task GetSummaryAsync_ExistingParticipant_ReturnsSummaryWithLocalizedCodes()
    {
        var participant = CreateTestParticipant();
        _repository.GetByIdAsync("LOC-001", Arg.Any<CancellationToken>()).Returns(participant);
        SetupDefaultCommonCodes();

        var result = await _service.GetSummaryAsync("LOC-001", "eng");

        result.Should().NotBeNull();
        result!.Id.Should().Be("LOC-001");
        result.Organisation.Code.Should().Be("USA");
        result.Organisation.Description.Long.Should().Be("United States of America");
        result.Gender.Code.Should().Be("M");
        result.Gender.Description.Long.Should().Be("Male");
        result.Functions.Should().HaveCount(1);
        result.PhotoUrl.Should().Be("https://example.com/photo.jpg");
    }

    [Fact]
    public async Task GetSummaryAsync_NonExistentParticipant_ReturnsNull()
    {
        _repository.GetByIdAsync("DOES-NOT-EXIST", Arg.Any<CancellationToken>()).Returns((Participant?)null);

        var result = await _service.GetSummaryAsync("DOES-NOT-EXIST", "eng");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSummariesAsync_Bulk_ReturnsAllSummaries()
    {
        var p1 = CreateTestParticipant("LOC-001");
        var p2 = CreateTestParticipant("LOC-002");
        var ids = new List<string> { "LOC-001", "LOC-002" };
        _repository.GetByIdsAsync(ids, Arg.Any<CancellationToken>()).Returns(new List<Participant> { p1, p2 });
        SetupDefaultCommonCodes();

        var result = await _service.GetSummariesAsync(ids, "eng");

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSummariesAsync_EmptyIds_ReturnsEmpty()
    {
        var result = await _service.GetSummariesAsync([], "eng");

        result.Should().BeEmpty();
        await _repository.DidNotReceive().GetByIdsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSummaryAsync_MissingCommonCode_FallsBackToCodeInDescription()
    {
        var participant = CreateTestParticipant();
        _repository.GetByIdAsync("LOC-001", Arg.Any<CancellationToken>()).Returns(participant);

        // All codes return null (not found in CommonCodes)
        _commonCodeReader.GetNameAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((MultilingualText?)null);

        var result = await _service.GetSummaryAsync("LOC-001", "eng");

        result.Should().NotBeNull();
        result!.Organisation.Code.Should().Be("USA");
        result.Organisation.Description.Long.Should().Be("USA");
        result.Gender.Code.Should().Be("M");
        result.Gender.Description.Long.Should().Be("M");
    }
}
