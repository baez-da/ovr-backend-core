using FluentAssertions;
using OVR.Modules.Scheduling.Domain;

namespace OVR.Modules.Scheduling.Tests.Domain;

public class SessionAggregateTests
{
    private static Session CreateValid(
        string code = "BOX01",
        string venueCode = "ABC",
        string name = "Boxing Session 1",
        DateTime? startDate = null,
        DateTime? endDate = null,
        TimeSpan? leadin = null)
    {
        var start = startDate ?? new DateTime(2026, 4, 20, 10, 0, 0, DateTimeKind.Utc);
        var end = endDate ?? start.AddHours(4);
        return Session.Create(code, venueCode, name, start, end, leadin);
    }

    [Fact]
    public void Create_WithValidInputs_SetsProperties()
    {
        var session = CreateValid(leadin: TimeSpan.FromMinutes(5));

        session.Id.Should().Be("BOX01");
        session.Code.Should().Be("BOX01");
        session.VenueCode.Should().Be("ABC");
        session.Name.Should().Be("Boxing Session 1");
        session.Leadin.Should().Be(TimeSpan.FromMinutes(5));
        session.StartDate.Should().Be(new DateTime(2026, 4, 20, 10, 0, 0, DateTimeKind.Utc));
        session.EndDate.Should().Be(new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc));
        session.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithNullLeadin_AllowsIt()
    {
        var session = CreateValid(leadin: null);

        session.Leadin.Should().BeNull();
    }

    [Fact]
    public void Create_WithEndDateBeforeStartDate_Throws()
    {
        var start = new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc);
        var end = start.AddHours(-1);

        Action act = () => CreateValid(startDate: start, endDate: end);

        act.Should().Throw<ArgumentException>().WithMessage("*EndDate*");
    }

    [Fact]
    public void Create_WithEndDateEqualToStartDate_Throws()
    {
        var start = new DateTime(2026, 4, 20, 10, 0, 0, DateTimeKind.Utc);

        Action act = () => CreateValid(startDate: start, endDate: start);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyCode_Throws()
    {
        Action act = () => CreateValid(code: "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithInvalidVenueLength_Throws()
    {
        Action act = () => CreateValid(venueCode: "AB");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNegativeLeadin_Throws()
    {
        Action act = () => CreateValid(leadin: TimeSpan.FromMinutes(-1));

        act.Should().Throw<ArgumentException>();
    }
}
