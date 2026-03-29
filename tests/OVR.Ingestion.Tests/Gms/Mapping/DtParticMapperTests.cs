using FluentAssertions;
using OVR.Ingestion.Gms.Mapping;
using OVR.Ingestion.Gms.Parsing;
using OVR.Modules.ParticipantRegistry.Domain.NameSystem;

namespace OVR.Ingestion.Tests.Gms.Mapping;

public class DtParticMapperTests
{
    private static readonly INameBuilder NameBuilder = new OdfNameBuilder();

    private static ParsedParticipant CreateAthlete() => new(
        Code: "1535186",
        GivenName: "Miguel Angel",
        FamilyName: "Martinez Ramirez",
        Gender: "M",
        Organisation: "MEX",
        BirthDate: "2001-03-17",
        MainFunctionId: "AA01",
        Status: "ACTIVE",
        Current: true,
        Nationality: "MEX",
        Height: 178,
        PassportGivenName: "MIGUEL",
        PassportFamilyName: "MARTINEZ RAMIREZ",
        PrintName: "MARTINEZ RAMIREZ Miguel Angel",
        PrintInitialName: "MARTINEZ RAMIREZ M",
        TVName: "Miguel Angel MARTINEZ RAMIREZ",
        TVInitialName: "M.MARTINEZ RAMIREZ",
        TVFamilyName: "MARTINEZ RAMIREZ",
        PscbName: null, PscbShortName: null, PscbLongName: null,
        PlaceofBirth: "DURANGO MEXICO",
        CountryofBirth: "MEX",
        PlaceofResidence: null, CountryofResidence: null,
        LocalGivenName: null, LocalFamilyName: null,
        OlympicSolidarity: null,
        Parent: "1535186",
        DisciplineInfo: new ParsedDiscipline(
            Code: "BOX-------------------------------",
            IFId: "MEX00024311",
            DisciplineEntries: [new ParsedKeyValue("STANCE", "L")],
            RegisteredEvents: [
                new ParsedRegisteredEvent(
                    Event: "BOXM63KG--------------------------",
                    Bib: null,
                    EventEntries: [
                        new ParsedKeyValue("QUAL_TYPE", "CNT"),
                        new ParsedKeyValue("SEED", "3")
                    ])
            ]));

    [Fact]
    public void MapParticipant_ShouldMapCoreFields()
    {
        var (participant, _) = DtParticMapper.Map(CreateAthlete(), NameBuilder);

        participant.Code.Should().Be("1535186");
        participant.BiographicData.FamilyName.Should().Be("Martinez Ramirez");
        participant.BiographicData.GivenName.Should().Be("Miguel Angel");
        participant.BiographicData.Gender.Value.Should().Be("M");
        participant.BiographicData.Organisation.Code.Should().Be("MEX");
        participant.BiographicData.BirthDate.Should().Be(new DateOnly(2001, 3, 17));
    }

    [Fact]
    public void MapParticipant_ShouldMapOdfFields()
    {
        var (participant, _) = DtParticMapper.Map(CreateAthlete(), NameBuilder);

        participant.Nationality.Should().Be("MEX");
        participant.Status.Should().Be("ACTIVE");
        participant.Current.Should().BeTrue();
        participant.PassportGivenName.Should().Be("MIGUEL");
        participant.PassportFamilyName.Should().Be("MARTINEZ RAMIREZ");
        participant.Height.Should().Be(178);
    }

    [Fact]
    public void MapParticipant_ShouldMapNameVariantsFromXml()
    {
        var (participant, _) = DtParticMapper.Map(CreateAthlete(), NameBuilder);

        participant.PrintName.Should().Be("MARTINEZ RAMIREZ Miguel Angel");
        participant.TvName.Should().Be("Miguel Angel MARTINEZ RAMIREZ");
    }

    [Fact]
    public void MapParticipant_ShouldGeneratePscbNamesWhenNull()
    {
        var (participant, _) = DtParticMapper.Map(CreateAthlete(), NameBuilder);

        participant.PscbName.Should().NotBeNullOrEmpty();
        participant.PscbShortName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void MapParticipant_ShouldMapFunctionWithIFId()
    {
        var (participant, _) = DtParticMapper.Map(CreateAthlete(), NameBuilder);

        participant.Functions.Should().ContainSingle();
        var fn = participant.Functions[0];
        fn.Function.Should().Be("AA01");
        fn.Discipline.Should().Be("BOX-------------------------------");
        fn.IsMain.Should().BeTrue();
        fn.IFId.Should().Be("MEX00024311");
    }

    [Fact]
    public void MapParticipant_ShouldPutDisciplineEntryInSupplementaryData()
    {
        var (participant, _) = DtParticMapper.Map(CreateAthlete(), NameBuilder);

        participant.SupplementaryData.Get("STANCE").Should().Be("L");
        participant.SupplementaryData.Get("PlaceofBirth").Should().Be("DURANGO MEXICO");
        participant.SupplementaryData.Get("CountryofBirth").Should().Be("MEX");
        participant.SupplementaryData.Get("Parent").Should().Be("1535186");
    }

    [Fact]
    public void MapParticipant_ShouldCreateEntriesFromRegisteredEvents()
    {
        var (_, entries) = DtParticMapper.Map(CreateAthlete(), NameBuilder);

        entries.Should().ContainSingle();
        var entry = entries[0];
        entry.EventRsc.Value.Should().Be("BOXM63KG--------------------------");
        entry.Organisation.Code.Should().Be("MEX");
        entry.Seed.Should().Be("3");
        entry.EventEntries.Should().ContainKey("QUAL_TYPE");
        entry.EventEntries!["QUAL_TYPE"].Should().Be("CNT");
    }

    [Fact]
    public void MapParticipant_Official_ShouldHaveNoEntries()
    {
        var official = CreateAthlete() with
        {
            Code = "9999",
            MainFunctionId = "JU",
            DisciplineInfo = new ParsedDiscipline("BOX-------------------------------", null, [], [])
        };

        var (_, entries) = DtParticMapper.Map(official, NameBuilder);
        entries.Should().BeEmpty();
    }
}
