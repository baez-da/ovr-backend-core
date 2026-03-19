using ErrorOr;
using MediatR;
using OVR.Modules.CommonCodes.Errors;
using OVR.Modules.CommonCodes.Persistence;
using OVR.SharedKernel.Domain.Events.Integration;

namespace OVR.Modules.CommonCodes.Features.ImportFromExcel;

public sealed class ImportFromExcelHandler(
    ICommonCodeRepository repository,
    IPublisher publisher)
    : IRequestHandler<ImportFromExcelCommand, ErrorOr<ImportResult>>
{
    public async Task<ErrorOr<ImportResult>> Handle(
        ImportFromExcelCommand request,
        CancellationToken cancellationToken)
    {
        ExcelParseResult parseResult;
        try
        {
            parseResult = ExcelParser.Parse(request.File);
        }
        catch (Exception ex)
        {
            return CommonCodeErrors.InvalidFile(ex.Message);
        }

        var isOverwrite = request.Strategy.Equals("overwrite", StringComparison.OrdinalIgnoreCase);
        var imported = new Dictionary<string, SheetImportResult>();
        var totalImported = 0;
        var totalErrors = 0;

        foreach (var sheet in parseResult.Sheets)
        {
            if (sheet.Documents.Count == 0)
            {
                imported[sheet.Type] = new SheetImportResult(0,
                    sheet.Errors.Select(e => new ImportError(e.Row, e.Column, e.Error)).ToList());
                totalErrors += sheet.Errors.Count;
                continue;
            }

            if (isOverwrite)
                await repository.DeleteByTypeAsync(sheet.Type, cancellationToken);

            await repository.UpsertManyAsync(sheet.Documents, cancellationToken);

            await publisher.Publish(new CommonCodesReimportedEvent(
                sheet.Type,
                request.Strategy,
                sheet.Documents.Select(d => d.Code).ToList()), cancellationToken);

            var sheetErrors = sheet.Errors.Select(e => new ImportError(e.Row, e.Column, e.Error)).ToList();
            imported[sheet.Type] = new SheetImportResult(sheet.Documents.Count, sheetErrors);
            totalImported += sheet.Documents.Count;
            totalErrors += sheetErrors.Count;
        }

        return new ImportResult(imported, parseResult.SkippedSheets, request.Strategy, totalImported, totalErrors);
    }
}
