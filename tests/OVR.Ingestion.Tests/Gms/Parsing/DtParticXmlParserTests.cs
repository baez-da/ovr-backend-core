using FluentAssertions;
using OVR.Ingestion.Gms.Parsing;

namespace OVR.Ingestion.Tests.Gms.Parsing;

public class DtParticXmlParserTests
{
    private static DtParticDocument ParseTestFile()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "valid-dt-partic.xml");
        using var stream = File.OpenRead(path);
        return DtParticXmlParser.Parse(stream);
    }

    [Fact]
    public void Parse_ShouldExtractHeader()
    {
        var doc = ParseTestFile();

        doc.CompetitionCode.Should().Be("OG2024");
        doc.Discipline.Should().Be("BOX-------------------------------");
        doc.Version.Should().Be(6);
        doc.FeedFlag.Should().Be("P");
    }

    [Fact]
    public void Parse_ShouldExtractAllParticipants()
    {
        var doc = ParseTestFile();
        doc.Participants.Should().HaveCount(3);
    }

    [Fact]
    public void Parse_Athlete_ShouldExtractAllFields()
    {
        var doc = ParseTestFile();
        var athlete = doc.Participants.First(p => p.Code == "1535186");

        athlete.GivenName.Should().Be("Miguel Angel");
        athlete.FamilyName.Should().Be("Martinez Ramirez");
        athlete.Gender.Should().Be("M");
        athlete.Organisation.Should().Be("MEX");
        athlete.BirthDate.Should().Be("2001-03-17");
        athlete.MainFunctionId.Should().Be("AA01");
        athlete.Status.Should().Be("ACTIVE");
        athlete.Current.Should().BeTrue();
        athlete.Nationality.Should().Be("MEX");
        athlete.Height.Should().Be(178);
        athlete.PassportGivenName.Should().Be("MIGUEL");
        athlete.PassportFamilyName.Should().Be("MARTINEZ RAMIREZ");
        athlete.PrintName.Should().Be("MARTINEZ RAMIREZ Miguel Angel");
        athlete.PlaceofBirth.Should().Be("DURANGO MEXICO");
        athlete.CountryofBirth.Should().Be("MEX");
        athlete.Parent.Should().Be("1535186");
    }

    [Fact]
    public void Parse_Athlete_ShouldExtractDisciplineWithIFId()
    {
        var doc = ParseTestFile();
        var athlete = doc.Participants.First(p => p.Code == "1535186");

        athlete.DisciplineInfo.Code.Should().Be("BOX-------------------------------");
        athlete.DisciplineInfo.IFId.Should().Be("MEX00024311");
    }

    [Fact]
    public void Parse_Athlete_ShouldExtractDisciplineEntries()
    {
        var doc = ParseTestFile();
        var athlete = doc.Participants.First(p => p.Code == "1535186");

        athlete.DisciplineInfo.DisciplineEntries.Should().ContainSingle();
        athlete.DisciplineInfo.DisciplineEntries[0].Code.Should().Be("STANCE");
        athlete.DisciplineInfo.DisciplineEntries[0].Value.Should().Be("L");
    }

    [Fact]
    public void Parse_Athlete_ShouldExtractRegisteredEvents()
    {
        var doc = ParseTestFile();
        var athlete = doc.Participants.First(p => p.Code == "1535186");

        athlete.DisciplineInfo.RegisteredEvents.Should().ContainSingle();
        var regEvent = athlete.DisciplineInfo.RegisteredEvents[0];
        regEvent.Event.Should().Be("BOXM63KG--------------------------");
        regEvent.EventEntries.Should().HaveCount(2);
        regEvent.EventEntries.Should().Contain(e => e.Code == "QUAL_TYPE" && e.Value == "CNT");
        regEvent.EventEntries.Should().Contain(e => e.Code == "SEED" && e.Value == "3");
    }

    [Fact]
    public void Parse_Official_ShouldHaveNoRegisteredEvents()
    {
        var doc = ParseTestFile();
        var official = doc.Participants.First(p => p.Code == "1958435");

        official.MainFunctionId.Should().Be("JU");
        official.DisciplineInfo.RegisteredEvents.Should().BeEmpty();
    }

    [Fact]
    public void Parse_CancelledParticipant_ShouldHaveCancelStatus()
    {
        var doc = ParseTestFile();
        var cancelled = doc.Participants.First(p => p.Code == "1979696");

        cancelled.Status.Should().Be("CANCEL");
    }

    [Fact]
    public void Parse_InvalidDocumentType_ShouldThrow()
    {
        var xml = """<?xml version="1.0"?><OdfBody DocumentType="DT_RESULT"><Competition/></OdfBody>""";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));

        var act = () => DtParticXmlParser.Parse(stream);
        act.Should().Throw<InvalidOperationException>().WithMessage("*DT_PARTIC*");
    }

    [Fact]
    public void Parse_MalformedXml_ShouldThrow()
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("not xml"));

        var act = () => DtParticXmlParser.Parse(stream);
        act.Should().Throw<Exception>();
    }
}
