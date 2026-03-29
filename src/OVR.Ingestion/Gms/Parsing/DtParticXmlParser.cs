using System.Xml.Linq;

namespace OVR.Ingestion.Gms.Parsing;

public static class DtParticXmlParser
{
    public static DtParticDocument Parse(Stream xml)
    {
        var doc = XDocument.Load(xml);
        var body = doc.Root
            ?? throw new InvalidOperationException("XML has no root element.");

        var documentType = body.Attribute("DocumentType")?.Value
            ?? throw new InvalidOperationException("Missing DocumentType attribute.");

        if (documentType != "DT_PARTIC")
            throw new InvalidOperationException(
                $"Expected DocumentType 'DT_PARTIC' but got '{documentType}'.");

        var competition = body.Element("Competition")
            ?? throw new InvalidOperationException("Missing Competition element.");

        var participants = competition.Elements("Participant")
            .Select(ParseParticipant)
            .ToList();

        return new DtParticDocument(
            CompetitionCode: body.Attribute("CompetitionCode")?.Value ?? "",
            Discipline: body.Attribute("DocumentCode")?.Value ?? "",
            Version: int.TryParse(body.Attribute("Version")?.Value, out var v) ? v : 0,
            FeedFlag: body.Attribute("FeedFlag")?.Value ?? "P",
            Participants: participants);
    }

    private static ParsedParticipant ParseParticipant(XElement el)
    {
        var discipline = el.Element("Discipline");

        return new ParsedParticipant(
            Code: el.Attribute("Code")?.Value ?? "",
            GivenName: el.Attribute("GivenName")?.Value,
            FamilyName: el.Attribute("FamilyName")?.Value ?? "",
            Gender: el.Attribute("Gender")?.Value ?? "",
            Organisation: el.Attribute("Organisation")?.Value ?? "",
            BirthDate: el.Attribute("BirthDate")?.Value,
            MainFunctionId: el.Attribute("MainFunctionId")?.Value ?? "",
            Status: el.Attribute("Status")?.Value ?? "",
            Current: el.Attribute("Current")?.Value == "true",
            Nationality: el.Attribute("Nationality")?.Value,
            Height: int.TryParse(el.Attribute("Height")?.Value, out var h) ? h : null,
            PassportGivenName: el.Attribute("PassportGivenName")?.Value,
            PassportFamilyName: el.Attribute("PassportFamilyName")?.Value,
            PrintName: el.Attribute("PrintName")?.Value ?? "",
            PrintInitialName: el.Attribute("PrintInitialName")?.Value ?? "",
            TVName: el.Attribute("TVName")?.Value ?? "",
            TVInitialName: el.Attribute("TVInitialName")?.Value ?? "",
            TVFamilyName: el.Attribute("TVFamilyName")?.Value ?? "",
            PscbName: el.Attribute("PSCBName")?.Value,
            PscbShortName: el.Attribute("PSCBShortName")?.Value,
            PscbLongName: el.Attribute("PSCBLongName")?.Value,
            PlaceofBirth: el.Attribute("PlaceofBirth")?.Value,
            CountryofBirth: el.Attribute("CountryofBirth")?.Value,
            PlaceofResidence: el.Attribute("PlaceofResidence")?.Value,
            CountryofResidence: el.Attribute("CountryofResidence")?.Value,
            LocalGivenName: el.Attribute("LocalGivenName")?.Value,
            LocalFamilyName: el.Attribute("LocalFamilyName")?.Value,
            OlympicSolidarity: el.Attribute("OlympicSolidarity")?.Value,
            Parent: el.Attribute("Parent")?.Value ?? "",
            DisciplineInfo: ParseDiscipline(discipline));
    }

    private static ParsedDiscipline ParseDiscipline(XElement? el)
    {
        if (el is null)
            return new ParsedDiscipline("", null, [], []);

        var disciplineEntries = el.Elements("DisciplineEntry")
            .Select(e => new ParsedKeyValue(
                e.Attribute("Code")?.Value ?? "",
                e.Attribute("Value")?.Value ?? ""))
            .ToList();

        var registeredEvents = el.Elements("RegisteredEvent")
            .Select(ParseRegisteredEvent)
            .ToList();

        return new ParsedDiscipline(
            Code: el.Attribute("Code")?.Value ?? "",
            IFId: el.Attribute("IFId")?.Value,
            DisciplineEntries: disciplineEntries,
            RegisteredEvents: registeredEvents);
    }

    private static ParsedRegisteredEvent ParseRegisteredEvent(XElement el)
    {
        var eventEntries = el.Elements("EventEntry")
            .Select(e => new ParsedKeyValue(
                e.Attribute("Code")?.Value ?? "",
                e.Attribute("Value")?.Value ?? ""))
            .ToList();

        return new ParsedRegisteredEvent(
            Event: el.Attribute("Event")?.Value ?? "",
            Bib: el.Attribute("Bib")?.Value,
            EventEntries: eventEntries);
    }
}
