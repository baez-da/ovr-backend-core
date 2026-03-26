using FluentAssertions;
using NSubstitute;
using OVR.Modules.ParticipantRegistry.Domain;
using OVR.Modules.ParticipantRegistry.Features.GetParticipant;
using OVR.Modules.ParticipantRegistry.Persistence;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.ParticipantRegistry.Tests.Features.GetParticipant;

public class GetParticipantHandlerTests
{
    private readonly IParticipantRepository _repository = Substitute.For<IParticipantRepository>();
    private readonly GetParticipantHandler _handler;

    public GetParticipantHandlerTests()
    {
        _handler = new GetParticipantHandler(_repository);
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
    public async Task Handle_ExistingParticipant_ReturnsResponseWithCodes()
    {
        var participant = CreateTestParticipant();
        _repository.GetByIdAsync("LOC-001", Arg.Any<CancellationToken>()).Returns(participant);

        var query = new GetParticipantQuery("LOC-001");
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Id.Should().Be("LOC-001");
        result.Value.Organisation.Should().Be("USA");
        result.Value.Gender.Should().Be("M");
        result.Value.Functions.Should().HaveCount(1);
        result.Value.Functions[0].Function.Should().Be("ATH");
        result.Value.Functions[0].Discipline.Should().Be("SWM");
        result.Value.PhotoUrl.Should().Be("https://example.com/photo.jpg");
    }

    [Fact]
    public async Task Handle_NonExistentParticipant_ReturnsNotFoundError()
    {
        _repository.GetByIdAsync("DOES-NOT-EXIST", Arg.Any<CancellationToken>()).Returns((Participant?)null);

        var query = new GetParticipantQuery("DOES-NOT-EXIST");
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Participant.NotFound");
    }
}
