using ClosedXML.Excel;
using FluentAssertions;
using MediatR;
using NSubstitute;
using OVR.Modules.CommonCodes.Features.ImportFromExcel;
using OVR.Modules.CommonCodes.Persistence;
using OVR.SharedKernel.Domain.Events.Integration;

namespace OVR.Modules.CommonCodes.Tests.Features.ImportFromExcel;

public class ImportFromExcelHandlerTests
{
    private readonly ICommonCodeRepository _repository = Substitute.For<ICommonCodeRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    [Fact]
    public async Task Handle_OverwriteStrategy_ShouldDeleteThenUpsert()
    {
        var handler = new ImportFromExcelHandler(_repository, _publisher);
        var stream = CreateTestExcel("SPORT", ("BOX", "Boxing"));

        var result = await handler.Handle(
            new ImportFromExcelCommand(stream, "overwrite"), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.TotalImported.Should().Be(1);
        await _repository.Received(1).DeleteByTypeAsync("SPORT", Arg.Any<CancellationToken>());
        await _repository.Received(1).UpsertManyAsync(Arg.Any<IReadOnlyList<CommonCodeDocument>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UpdateStrategy_ShouldUpsertWithoutDelete()
    {
        var handler = new ImportFromExcelHandler(_repository, _publisher);
        var stream = CreateTestExcel("SPORT", ("BOX", "Boxing"));

        var result = await handler.Handle(
            new ImportFromExcelCommand(stream, "update"), CancellationToken.None);

        result.IsError.Should().BeFalse();
        await _repository.DidNotReceive().DeleteByTypeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).UpsertManyAsync(Arg.Any<IReadOnlyList<CommonCodeDocument>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPublishEventPerSheet()
    {
        var handler = new ImportFromExcelHandler(_repository, _publisher);
        var stream = CreateTestExcel("SPORT", ("BOX", "Boxing"));

        await handler.Handle(
            new ImportFromExcelCommand(stream, "overwrite"), CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Any<CommonCodesReimportedEvent>(),
            Arg.Any<CancellationToken>());
    }

    private static MemoryStream CreateTestExcel(string sheetName, params (string Code, string Desc)[] rows)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet(sheetName);
        ws.Cell(1, 1).Value = "Id";
        ws.Cell(1, 2).Value = "ENG Description";
        for (var i = 0; i < rows.Length; i++)
        {
            ws.Cell(i + 2, 1).Value = rows[i].Code;
            ws.Cell(i + 2, 2).Value = rows[i].Desc;
        }
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
