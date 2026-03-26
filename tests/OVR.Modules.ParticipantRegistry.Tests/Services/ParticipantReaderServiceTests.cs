using FluentAssertions;
using NSubstitute;
using OVR.Modules.ParticipantRegistry.Domain;
using OVR.Modules.ParticipantRegistry.Persistence;
using OVR.Modules.ParticipantRegistry.Services;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.ParticipantRegistry.Tests.Services;

public class ParticipantReaderServiceTests
{
    private readonly IParticipantRepository _repository = Substitute.For<IParticipantRepository>();
    private readonly ParticipantReaderService _service;

    public ParticipantReaderServiceTests()
    {
        _service = new ParticipantReaderService(_repository);
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
    public async Task GetSummaryAsync_ExistingParticipant_ReturnsSummaryWithCodes()
    {
        var participant = CreateTestParticipant();
        _repository.GetByIdAsync("LOC-001", Arg.Any<CancellationToken>()).Returns(participant);

        var result = await _service.GetSummaryAsync("LOC-001");

        result.Should().NotBeNull();
        result!.Id.Should().Be("LOC-001");
        result.Organisation.Should().Be("USA");
        result.Gender.Should().Be("M");
        result.Functions.Should().HaveCount(1);
        result.Functions[0].Function.Should().Be("ATH");
        result.Functions[0].Discipline.Should().Be("SWM");
        result.PhotoUrl.Should().Be("https://example.com/photo.jpg");
    }

    [Fact]
    public async Task GetSummaryAsync_NonExistentParticipant_ReturnsNull()
    {
        _repository.GetByIdAsync("DOES-NOT-EXIST", Arg.Any<CancellationToken>()).Returns((Participant?)null);

        var result = await _service.GetSummaryAsync("DOES-NOT-EXIST");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSummariesAsync_Bulk_ReturnsAllSummaries()
    {
        var p1 = CreateTestParticipant("LOC-001");
        var p2 = CreateTestParticipant("LOC-002");
        var ids = new List<string> { "LOC-001", "LOC-002" };
        _repository.GetByIdsAsync(ids, Arg.Any<CancellationToken>()).Returns(new List<Participant> { p1, p2 });

        var result = await _service.GetSummariesAsync(ids);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSummariesAsync_EmptyIds_ReturnsEmpty()
    {
        var result = await _service.GetSummariesAsync([]);

        result.Should().BeEmpty();
        await _repository.DidNotReceive().GetByIdsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }
}
