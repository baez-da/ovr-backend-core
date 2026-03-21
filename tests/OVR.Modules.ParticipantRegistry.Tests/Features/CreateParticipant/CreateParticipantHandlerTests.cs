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

    private static List<FunctionDto> DefaultFunctions() =>
        [new("ATH", "SWM", true)];

    private CreateParticipantCommand ValidCommand(
        string? givenName = "John", string organisation = "USA", List<FunctionDto>? functions = null) =>
        new(givenName, "Smith", "M", null, organisation, functions ?? DefaultFunctions());

    private void SetupValidCommonCodes(string organisation = "USA")
    {
        _commonCodeReader.ExistsAsync("delegation", organisation, Arg.Any<CancellationToken>()).Returns(true);
        _commonCodeReader.ExistsAsync("discipline", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _commonCodeReader.ExistsAsync("discipline_function", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateParticipantWithLocPrefix()
    {
        SetupValidCommonCodes();

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Id.Should().StartWith("LOC-");
        result.Value.PrintName.Should().Be("SMITH John");
        result.Value.TvName.Should().Be("John SMITH");
        result.Value.Functions.Should().ContainSingle(f => f.FunctionId == "ATH" && f.IsMain);
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
    public async Task Handle_InvalidFunction_ShouldReturnError()
    {
        _commonCodeReader.ExistsAsync("delegation", "USA", Arg.Any<CancellationToken>()).Returns(true);
        _commonCodeReader.ExistsAsync("discipline", "SWM", Arg.Any<CancellationToken>()).Returns(true);
        _commonCodeReader.ExistsAsync("discipline_function", "INVALID", Arg.Any<CancellationToken>()).Returns(false);

        var functions = new List<FunctionDto> { new("INVALID", "SWM", true) };
        var result = await _handler.Handle(ValidCommand(functions: functions), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Participant.InvalidFunction");
    }

    [Fact]
    public async Task Handle_InvalidDiscipline_ShouldReturnError()
    {
        _commonCodeReader.ExistsAsync("delegation", "USA", Arg.Any<CancellationToken>()).Returns(true);
        _commonCodeReader.ExistsAsync("discipline", "ZZZ", Arg.Any<CancellationToken>()).Returns(false);

        var functions = new List<FunctionDto> { new("ATH", "ZZZ", true) };
        var result = await _handler.Handle(ValidCommand(functions: functions), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Participant.InvalidDiscipline");
    }

    [Fact]
    public async Task Handle_NullGivenName_ShouldCreateSingleNameParticipant()
    {
        SetupValidCommonCodes("BRA");

        var result = await _handler.Handle(ValidCommand(givenName: null, organisation: "BRA"), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.PrintName.Should().Be("SMITH");
        result.Value.TvName.Should().Be("SMITH");
    }

    [Fact]
    public async Task Handle_MultipleFunctions_ShouldCreateParticipant()
    {
        SetupValidCommonCodes();

        var functions = new List<FunctionDto>
        {
            new("COACH", "VVO", true),
            new("TM_MGR", "VVO", false)
        };
        var result = await _handler.Handle(ValidCommand(functions: functions), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Functions.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldDispatchDomainEvents()
    {
        SetupValidCommonCodes();

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _publisher.Received(1).Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnAllEightNames()
    {
        SetupValidCommonCodes();

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
