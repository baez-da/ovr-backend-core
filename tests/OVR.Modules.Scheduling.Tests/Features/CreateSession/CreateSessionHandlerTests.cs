using FluentAssertions;
using NSubstitute;
using OVR.Modules.CommonCodes.Contracts;
using OVR.Modules.Scheduling.Domain;
using OVR.Modules.Scheduling.Features.CreateSession;
using OVR.Modules.Scheduling.Persistence;
using OVR.SharedKernel.Contracts;

namespace OVR.Modules.Scheduling.Tests.Features.CreateSession;

public class CreateSessionHandlerTests
{
    private readonly ISessionRepository _repo = Substitute.For<ISessionRepository>();
    private readonly ICommonCodeCache _cache = Substitute.For<ICommonCodeCache>();
    private readonly CreateSessionHandler _handler;

    public CreateSessionHandlerTests()
    {
        _handler = new CreateSessionHandler(_repo, _cache);
    }

    private static CreateSessionCommand ValidCommand() =>
        new(
            Code: "BOX01",
            VenueCode: "ABC",
            Name: "Boxing Session 1",
            StartDate: new DateTime(2026, 4, 20, 10, 0, 0, DateTimeKind.Utc),
            EndDate: new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc),
            Leadin: TimeSpan.FromMinutes(5));

    private void SetupValidVenue() =>
        _cache.Exists(CommonCodeTypes.Venue, "ABC").Returns(true);

    [Fact]
    public async Task Handle_ValidCommand_PersistsAndReturnsResponse()
    {
        SetupValidVenue();

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Code.Should().Be("BOX01");
        await _repo.Received(1).AddAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidVenue_ReturnsInvalidVenueError()
    {
        _cache.Exists(CommonCodeTypes.Venue, "ABC").Returns(false);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Scheduling.InvalidVenue");
        await _repo.DidNotReceive().AddAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateCode_ReturnsSessionAlreadyExists()
    {
        SetupValidVenue();
        var existing = Session.Create("BOX01", "ABC", "existing",
            new DateTime(2026, 4, 20, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc),
            null);
        _repo.GetByCodeAsync("BOX01", Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Scheduling.SessionAlreadyExists");
    }
}
