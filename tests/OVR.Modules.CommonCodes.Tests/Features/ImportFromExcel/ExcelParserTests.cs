using ClosedXML.Excel;
using FluentAssertions;
using OVR.Modules.CommonCodes.Features.ImportFromExcel;

namespace OVR.Modules.CommonCodes.Tests.Features.ImportFromExcel;

public class ExcelParserTests
{
    private static MemoryStream CreateWorkbook(Action<XLWorkbook> configure)
    {
        using var workbook = new XLWorkbook();
        configure(workbook);
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public void Parse_StandardSheet_ExtractsCodeAndName()
    {
        using var stream = CreateWorkbook(wb =>
        {
            var ws = wb.AddWorksheet("Sport");
            ws.Cell(1, 1).Value = "Id";
            ws.Cell(1, 2).Value = "ENG Description";
            ws.Cell(1, 3).Value = "ENG longdescription";

            ws.Cell(2, 1).Value = "BX";
            ws.Cell(2, 2).Value = "Boxing Desc";
            ws.Cell(2, 3).Value = "Boxing Long";

            ws.Cell(3, 1).Value = "SW";
            ws.Cell(3, 2).Value = "Swimming Desc";
            ws.Cell(3, 3).Value = "Swimming Long";
        });

        var result = ExcelParser.Parse(stream);

        result.Sheets.Should().HaveCount(1);
        var sheet = result.Sheets[0];
        sheet.Type.Should().Be("SPORT");
        sheet.Documents.Should().HaveCount(2);

        var bx = sheet.Documents.First(d => d.Code == "BX");
        bx.Id.Should().Be("SPORT:BX");
        bx.Name.Should().ContainKey("eng");
        // Long Description takes priority over Description
        bx.Name["eng"].Long.Should().Be("Boxing Long");
    }

    [Fact]
    public void Parse_ShortDescriptionDetection_ExtractsLongAndShort()
    {
        using var stream = CreateWorkbook(wb =>
        {
            var ws = wb.AddWorksheet("Sport");
            ws.Cell(1, 1).Value = "Code";
            ws.Cell(1, 2).Value = "ENG Description";
            ws.Cell(1, 3).Value = "ENG Short Description";

            ws.Cell(2, 1).Value = "BX";
            ws.Cell(2, 2).Value = "Boxing";
            ws.Cell(2, 3).Value = "BOX";
        });

        var result = ExcelParser.Parse(stream);

        var doc = result.Sheets[0].Documents[0];
        doc.Name["eng"].Long.Should().Be("Boxing");
        doc.Name["eng"].Short.Should().Be("BOX");
    }

    [Fact]
    public void Parse_AttributeExtraction_ExtractsCodeAttributesAndName()
    {
        using var stream = CreateWorkbook(wb =>
        {
            var ws = wb.AddWorksheet("Venue");
            ws.Cell(1, 1).Value = "Venue Code";
            ws.Cell(1, 2).Value = "IndoorOutdoor";
            ws.Cell(1, 3).Value = "ENG longdescription";

            ws.Cell(2, 1).Value = "AQC";
            ws.Cell(2, 2).Value = "Indoor";
            ws.Cell(2, 3).Value = "Aquatics Centre";
        });

        var result = ExcelParser.Parse(stream);

        var doc = result.Sheets[0].Documents[0];
        doc.Code.Should().Be("AQC");
        doc.Attributes.Should().ContainKey("IndoorOutdoor");
        doc.Attributes["IndoorOutdoor"].Should().Be("Indoor");
        doc.Name["eng"].Long.Should().Be("Aquatics Centre");
    }

    [Fact]
    public void Parse_SkipsMetadataSheets_ReportsInSkippedSheets()
    {
        using var stream = CreateWorkbook(wb =>
        {
            var cover = wb.AddWorksheet("Cover");
            cover.Cell(1, 1).Value = "Title";

            var docControl = wb.AddWorksheet("Document Control");
            docControl.Cell(1, 1).Value = "Version";

            var changeLog = wb.AddWorksheet("Change Log Detail");
            changeLog.Cell(1, 1).Value = "Date";

            var sport = wb.AddWorksheet("Sport");
            sport.Cell(1, 1).Value = "Id";
            sport.Cell(1, 2).Value = "ENG Description";
            sport.Cell(2, 1).Value = "BX";
            sport.Cell(2, 2).Value = "Boxing";
        });

        var result = ExcelParser.Parse(stream);

        result.Sheets.Should().HaveCount(1);
        result.Sheets[0].Type.Should().Be("SPORT");
        result.SkippedSheets.Should().HaveCount(3);
        result.SkippedSheets.Should().Contain("Cover");
        result.SkippedSheets.Should().Contain("Document Control");
        result.SkippedSheets.Should().Contain("Change Log Detail");
    }

    [Fact]
    public void Parse_NoOrderColumn_UsesRowPosition()
    {
        using var stream = CreateWorkbook(wb =>
        {
            var ws = wb.AddWorksheet("Sport");
            ws.Cell(1, 1).Value = "Id";
            ws.Cell(1, 2).Value = "ENG Description";

            ws.Cell(2, 1).Value = "BX";
            ws.Cell(2, 2).Value = "Boxing";
            ws.Cell(3, 1).Value = "SW";
            ws.Cell(3, 2).Value = "Swimming";
            ws.Cell(4, 1).Value = "AT";
            ws.Cell(4, 2).Value = "Athletics";
        });

        var result = ExcelParser.Parse(stream);

        result.Sheets[0].Documents[0].Order.Should().Be(1);
        result.Sheets[0].Documents[1].Order.Should().Be(2);
        result.Sheets[0].Documents[2].Order.Should().Be(3);
    }

    [Fact]
    public void Parse_MultipleLanguages_ExtractsMultilingualName()
    {
        using var stream = CreateWorkbook(wb =>
        {
            var ws = wb.AddWorksheet("Sport");
            ws.Cell(1, 1).Value = "Id";
            ws.Cell(1, 2).Value = "ENG Description";
            ws.Cell(1, 3).Value = "FRA Description";
            ws.Cell(1, 4).Value = "ARB Description";

            ws.Cell(2, 1).Value = "BX";
            ws.Cell(2, 2).Value = "Boxing";
            ws.Cell(2, 3).Value = "Boxe";
            ws.Cell(2, 4).Value = "\u0645\u0644\u0627\u0643\u0645\u0629";
        });

        var result = ExcelParser.Parse(stream);

        var doc = result.Sheets[0].Documents[0];
        doc.Name.Should().HaveCount(3);
        doc.Name["eng"].Long.Should().Be("Boxing");
        doc.Name["fra"].Long.Should().Be("Boxe");
        doc.Name["arb"].Long.Should().Be("\u0645\u0644\u0627\u0643\u0645\u0629");
    }

    [Fact]
    public void Parse_EmptyCodeRow_ReportsErrorAndContinues()
    {
        using var stream = CreateWorkbook(wb =>
        {
            var ws = wb.AddWorksheet("Sport");
            ws.Cell(1, 1).Value = "Id";
            ws.Cell(1, 2).Value = "ENG Description";

            ws.Cell(2, 1).Value = "BX";
            ws.Cell(2, 2).Value = "Boxing";
            ws.Cell(3, 1).Value = "";    // empty code
            ws.Cell(3, 2).Value = "Swimming";
            ws.Cell(4, 1).Value = "AT";
            ws.Cell(4, 2).Value = "Athletics";
        });

        var result = ExcelParser.Parse(stream);

        var sheet = result.Sheets[0];
        sheet.Documents.Should().HaveCount(2);
        sheet.Documents.Select(d => d.Code).Should().BeEquivalentTo(["BX", "AT"]);
        sheet.Errors.Should().ContainSingle();
        sheet.Errors[0].Row.Should().Be(3);
    }

    [Fact]
    public void Parse_DuplicateCodeRow_ReportsErrorAndKeepsFirst()
    {
        using var stream = CreateWorkbook(wb =>
        {
            var ws = wb.AddWorksheet("Sport");
            ws.Cell(1, 1).Value = "Id";
            ws.Cell(1, 2).Value = "ENG Description";

            ws.Cell(2, 1).Value = "BX";
            ws.Cell(2, 2).Value = "Boxing";
            ws.Cell(3, 1).Value = "BX";  // duplicate
            ws.Cell(3, 2).Value = "Boxing Again";
            ws.Cell(4, 1).Value = "SW";
            ws.Cell(4, 2).Value = "Swimming";
        });

        var result = ExcelParser.Parse(stream);

        var sheet = result.Sheets[0];
        sheet.Documents.Should().HaveCount(2);
        sheet.Documents.First(d => d.Code == "BX").Name["eng"].Long.Should().Be("Boxing");
        sheet.Errors.Should().ContainSingle();
        sheet.Errors[0].Row.Should().Be(3);
    }

    [Fact]
    public void Parse_NoCodeColumn_ReportsErrorNoDocs()
    {
        using var stream = CreateWorkbook(wb =>
        {
            var ws = wb.AddWorksheet("Sport");
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "ENG Description";

            ws.Cell(2, 1).Value = "Boxing";
            ws.Cell(2, 2).Value = "Boxing";
        });

        var result = ExcelParser.Parse(stream);

        var sheet = result.Sheets[0];
        sheet.Documents.Should().BeEmpty();
        sheet.Errors.Should().ContainSingle();
        sheet.Errors[0].Error.Should().Contain("code column");
    }
}
