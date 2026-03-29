using FluentAssertions;
using OVR.Modules.ParticipantRegistry.Domain;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.ParticipantRegistry.Tests.Domain;

public class ParticipantTests
{
    private static readonly BiographicData TestBiographicData =
        BiographicData.Create("John", "Smith", Gender.FromCode("M"), null, Organisation.Create("USA"));

    private static readonly List<ParticipantFunction> SingleFunction =
        [ParticipantFunction.Create("ATH", "SWM", true)];

    private static readonly List<ParticipantFunction> MultipleFunctionsSameDiscipline =
    [
        ParticipantFunction.Create("COACH", "VVO", true),
        ParticipantFunction.Create("TM_MGR", "VVO", false)
    ];

    private static readonly List<ParticipantFunction> MultipleDisciplines =
    [
        ParticipantFunction.Create("COACH", "VVO", true),
        ParticipantFunction.Create("UMPIRE", "BVO", true)
    ];

    [Fact]
    public void Create_ShouldGenerateIdWithLocPrefix()
    {
        var participant = Participant.Create(
            TestBiographicData, null, SingleFunction,
            "SMITH John", "SMITH J", "John SMITH", "J. SMITH",
            "SMITH", "SMITH John", "SMITH J", "SMITH John");

        participant.Id.Should().StartWith("LOC-");
        participant.ParticipantId.Value.Should().StartWith("LOC-");
    }

    [Fact]
    public void Create_ShouldRaiseParticipantCreatedEvent()
    {
        var participant = Participant.Create(
            TestBiographicData, null, SingleFunction,
            "SMITH John", "SMITH J", "John SMITH", "J. SMITH",
            "SMITH", "SMITH John", "SMITH J", "SMITH John");

        participant.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void Create_ShouldStoreFunctions()
    {
        var participant = Participant.Create(
            TestBiographicData, null, MultipleFunctionsSameDiscipline,
            "p", "pi", "tv", "tvi", "tvf", "pscb", "pscbs", "pscbl");

        participant.Functions.Should().HaveCount(2);
        participant.Functions.Should().Contain(f => f.Function == "COACH" && f.IsMain);
        participant.Functions.Should().Contain(f => f.Function == "TM_MGR" && !f.IsMain);
    }

    [Fact]
    public void Create_MultipleDisciplines_ShouldSucceed()
    {
        var participant = Participant.Create(
            TestBiographicData, null, MultipleDisciplines,
            "p", "pi", "tv", "tvi", "tvf", "pscb", "pscbs", "pscbl");

        participant.Functions.Should().HaveCount(2);
    }

    [Fact]
    public void Create_EmptyFunctions_ShouldThrow()
    {
        var act = () => Participant.Create(
            TestBiographicData, null, [],
            "p", "pi", "tv", "tvi", "tvf", "pscb", "pscbs", "pscbl");

        act.Should().Throw<ArgumentException>().WithMessage("*At least one*");
    }

    [Fact]
    public void Create_TwoMainInSameDiscipline_ShouldThrow()
    {
        var functions = new List<ParticipantFunction>
        {
            ParticipantFunction.Create("COACH", "VVO", true),
            ParticipantFunction.Create("TM_MGR", "VVO", true)
        };

        var act = () => Participant.Create(
            TestBiographicData, null, functions,
            "p", "pi", "tv", "tvi", "tvf", "pscb", "pscbs", "pscbl");

        act.Should().Throw<ArgumentException>().WithMessage("*Exactly one main*");
    }

    [Fact]
    public void Create_NoMainInDiscipline_ShouldThrow()
    {
        var functions = new List<ParticipantFunction>
        {
            ParticipantFunction.Create("COACH", "VVO", false)
        };

        var act = () => Participant.Create(
            TestBiographicData, null, functions,
            "p", "pi", "tv", "tvi", "tvf", "pscb", "pscbs", "pscbl");

        act.Should().Throw<ArgumentException>().WithMessage("*Exactly one main*");
    }

    [Fact]
    public void Create_DuplicateFunctionDiscipline_ShouldThrow()
    {
        var functions = new List<ParticipantFunction>
        {
            ParticipantFunction.Create("COACH", "VVO", true),
            ParticipantFunction.Create("COACH", "VVO", false)
        };

        var act = () => Participant.Create(
            TestBiographicData, null, functions,
            "p", "pi", "tv", "tvi", "tvf", "pscb", "pscbs", "pscbl");

        act.Should().Throw<ArgumentException>().WithMessage("*Duplicate*");
    }

    [Fact]
    public void Hydrate_ShouldAcceptAnyIdAndNotRaiseEvents()
    {
        var id = ParticipantId.Create("GMS-12345");
        var participant = Participant.Hydrate(
            id, TestBiographicData, null, SingleFunction,
            "p", "pi", "tv", "tvi", "tvf", "pscb", "pscbs", "pscbl",
            DateTime.UtcNow, null);

        participant.Id.Should().Be("GMS-12345");
        participant.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldStoreAllEightNames()
    {
        var participant = Participant.Create(
            TestBiographicData, null, SingleFunction,
            "printName", "printInitialName", "tvName", "tvInitialName",
            "tvFamilyName", "pscbName", "pscbShortName", "pscbLongName");

        participant.PrintName.Should().Be("printName");
        participant.PrintInitialName.Should().Be("printInitialName");
        participant.TvName.Should().Be("tvName");
        participant.TvInitialName.Should().Be("tvInitialName");
        participant.TvFamilyName.Should().Be("tvFamilyName");
        participant.PscbName.Should().Be("pscbName");
        participant.PscbShortName.Should().Be("pscbShortName");
        participant.PscbLongName.Should().Be("pscbLongName");
    }

    [Fact]
    public void CreateFromOdf_ShouldSetAllOdfFields()
    {
        var biographic = BiographicData.Create(
            "Miguel Angel", "Martinez Ramirez",
            Gender.FromCode("M"), new DateOnly(2001, 3, 17),
            Organisation.Create("MEX"));
        var functions = new List<ParticipantFunction>
        {
            ParticipantFunction.Create("AA01", "BOX-------------------------------", true)
        };

        var participant = Participant.CreateFromOdf(
            code: "1535186",
            biographicData: biographic,
            functions: functions,
            printName: "MARTINEZ RAMIREZ Miguel Angel",
            printInitialName: "MARTINEZ RAMIREZ M",
            tvName: "Miguel Angel MARTINEZ RAMIREZ",
            tvInitialName: "M.MARTINEZ RAMIREZ",
            tvFamilyName: "MARTINEZ RAMIREZ",
            pscbName: "MARTINEZ RAMIREZ MIGUEL ANGEL",
            pscbShortName: "MARTINEZ RAMIREZ M",
            pscbLongName: "MARTINEZ RAMIREZ MIGUEL ANGEL",
            nationality: "MEX",
            status: "ACTIVE",
            current: true,
            passportGivenName: "MIGUEL",
            passportFamilyName: "MARTINEZ RAMIREZ",
            height: 178,
            supplementaryData: null);

        participant.Code.Should().Be("1535186");
        participant.Nationality.Should().Be("MEX");
        participant.Status.Should().Be("ACTIVE");
        participant.Current.Should().BeTrue();
        participant.PassportGivenName.Should().Be("MIGUEL");
        participant.PassportFamilyName.Should().Be("MARTINEZ RAMIREZ");
        participant.Height.Should().Be(178);
        participant.Id.Should().NotBeNullOrEmpty();
        participant.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void CreateFromOdf_NullOptionals_ShouldSucceed()
    {
        var biographic = BiographicData.Create(
            null, "Smith", Gender.FromCode("M"), null,
            Organisation.Create("USA"));
        var functions = new List<ParticipantFunction>
        {
            ParticipantFunction.Create("JU", "BOX-------------------------------", true)
        };

        var participant = Participant.CreateFromOdf(
            code: "9999999",
            biographicData: biographic,
            functions: functions,
            printName: "SMITH", printInitialName: "SMITH",
            tvName: "SMITH", tvInitialName: "SMITH", tvFamilyName: "SMITH",
            pscbName: "SMITH", pscbShortName: "SMITH", pscbLongName: "SMITH",
            nationality: null, status: "ACTIVE", current: true,
            passportGivenName: null, passportFamilyName: null,
            height: null, supplementaryData: null);

        participant.Nationality.Should().BeNull();
        participant.PassportGivenName.Should().BeNull();
        participant.Height.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldDefaultNewFieldsToNull()
    {
        var participant = Participant.Create(
            TestBiographicData, null, SingleFunction,
            "p", "pi", "tv", "tvi", "tvf", "pscb", "pscbs", "pscbl");

        participant.Code.Should().BeNull();
        participant.Nationality.Should().BeNull();
        participant.Status.Should().BeNull();
        participant.Current.Should().BeTrue();
        participant.PassportGivenName.Should().BeNull();
        participant.PassportFamilyName.Should().BeNull();
        participant.Height.Should().BeNull();
    }
}
