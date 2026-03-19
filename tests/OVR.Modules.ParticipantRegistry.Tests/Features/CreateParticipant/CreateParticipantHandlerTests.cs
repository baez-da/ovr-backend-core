using FluentAssertions;
using NSubstitute;
using MediatR;
using OVR.Modules.CommonCodes.Contracts;
using OVR.Modules.ParticipantRegistry.Domain.NameSystem;
using OVR.Modules.ParticipantRegistry.Features.CreateParticipant;
using OVR.Modules.ParticipantRegistry.Persistence;

namespace OVR.Modules.ParticipantRegistry.Tests.Features.CreateParticipant;

public class CreateParticipantHandlerTests
{
    private readonly IParticipantRepository _repository = Substitute.For<IParticipantRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly ICommonCodeReader _commonCodeReader = Substitute.For<ICommonCodeReader>();
    private readonly INameBuilder _nameBuilder = new OdfNameBuilder();
    private readonly CreateParticipantHandler _handler;

    public CreateParticipantHandlerTests()
    {
        _handler = new CreateParticipantHandler(_repository, _publisher, _commonCodeReader, _nameBuilder);
    }

    private static CreateParticipantCommand ValidCommand(string? givenName = "John", string organisation = "USA") =>
        new("Athlete", givenName, "Smith", "M", null, organisation);

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateParticipantWithLocPrefix()
    {
        _commonCodeReader.ExistsAsync("delegation", "USA", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Id.Should().StartWith("LOC-");
        result.Value.PrintName.Should().Be("SMITH John");
        result.Value.TvName.Should().Be("John SMITH");
        await _repository.Received(1).AddAsync(Arg.Any<OVR.Modules.ParticipantRegistry.Domain.Participant>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidOrganisation_ShouldReturnError()
    {
        _commonCodeReader.ExistsAsync("delegation", "ZZZ", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(ValidCommand(organisation: "ZZZ"), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Participant.InvalidOrganisation");
        await _repository.DidNotReceive().AddAsync(Arg.Any<OVR.Modules.ParticipantRegistry.Domain.Participant>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NullGivenName_ShouldCreateSingleNameParticipant()
    {
        _commonCodeReader.ExistsAsync("delegation", "BRA", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(ValidCommand(givenName: null, organisation: "BRA"), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.PrintName.Should().Be("SMITH");
        result.Value.TvName.Should().Be("SMITH");
    }

    [Fact]
    public async Task Handle_ShouldDispatchDomainEvents()
    {
        _commonCodeReader.ExistsAsync("delegation", "USA", Arg.Any<CancellationToken>()).Returns(true);

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _publisher.Received(1).Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnAllEightNames()
    {
        _commonCodeReader.ExistsAsync("delegation", "USA", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.PrintName.Should().NotBeNullOrEmpty();
        result.Value.PrintInitialName.Should().NotBeNullOrEmpty();
        result.Value.TvName.Should().NotBeNullOrEmpty();
        result.Value.TvInitialName.Should().NotBeNullOrEmpty();
        result.Value.TvFamilyName.Should().NotBeNullOrEmpty();
        result.Value.PscbName.Should().NotBeNullOrEmpty();
        result.Value.PscbShortName.Should().NotBeNullOrEmpty();
        result.Value.PscbLongName.Should().NotBeNullOrEmpty();
    }
}
