using FluentAssertions;
using MediatR;
using NSubstitute;
using OVR.Modules.CommonCodes.Contracts;
using OVR.Modules.CompetitionConfig.Domain;
using OVR.Modules.CompetitionConfig.Features.CreateEvent;
using OVR.Modules.CompetitionConfig.Persistence;
using OVR.SharedKernel.Contracts;

namespace OVR.Modules.CompetitionConfig.Tests.Features.CreateEvent;

public class CreateEventHandlerTests
{
    private readonly IEventRepository _repository = Substitute.For<IEventRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly ICommonCodeCache _cache = Substitute.For<ICommonCodeCache>();
    private readonly CreateEventHandler _handler;

    public CreateEventHandlerTests()
    {
        _handler = new CreateEventHandler(_repository, _publisher, _cache);
    }

    private void SetupValidCodes()
    {
        _cache.Exists(CommonCodeTypes.Discipline, "BOX").Returns(true);
        _cache.Exists(CommonCodeTypes.Event, "57KG").Returns(true);
    }

    private static CreateEventCommand ValidCommand() =>
        new("BOX", "M", "57KG", null, "Men's 57kg");

    [Fact]
    public async Task Handle_ValidCommand_Returns201LikeResponseAndPersists()
    {
        SetupValidCodes();

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Rsc.Should().Be("BOXM57KG--------------------------");
        result.Value.Name.Should().Be("Men's 57kg");
        await _repository.Received(1).AddAsync(Arg.Any<Event>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidDiscipline_ReturnsError()
    {
        _cache.Exists(CommonCodeTypes.Discipline, "BOX").Returns(false);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CompetitionConfig.InvalidDiscipline");
        await _repository.DidNotReceive().AddAsync(Arg.Any<Event>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidEventCode_ReturnsError()
    {
        _cache.Exists(CommonCodeTypes.Discipline, "BOX").Returns(true);
        _cache.Exists(CommonCodeTypes.Event, "57KG").Returns(false);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CompetitionConfig.InvalidEventCode");
    }

    [Fact]
    public async Task Handle_DuplicateRsc_ReturnsConflict()
    {
        SetupValidCodes();
        var existing = Event.Create(
            OVR.SharedKernel.Domain.ValueObjects.Rsc.Create("BOXM57KG--------------------------"),
            "BOX",
            OVR.SharedKernel.Domain.ValueObjects.Gender.FromCode("M"),
            "57KG",
            null,
            "Men's 57kg");
        _repository.GetByRscAsync("BOXM57KG--------------------------", Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CompetitionConfig.EventAlreadyExists");
    }
}
