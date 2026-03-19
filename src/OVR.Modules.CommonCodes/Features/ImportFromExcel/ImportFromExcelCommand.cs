using ErrorOr;
using MediatR;

namespace OVR.Modules.CommonCodes.Features.ImportFromExcel;

public sealed record ImportFromExcelCommand(
    Stream File,
    string Strategy) : IRequest<ErrorOr<ImportResult>>;

public sealed record ImportResult(
    Dictionary<string, SheetImportResult> Imported,
    List<string> SkippedSheets,
    string Strategy,
    int TotalImported,
    int TotalErrors);

public sealed record SheetImportResult(
    int Count,
    List<ImportError> Errors);

public sealed record ImportError(
    int Row,
    string Column,
    string Error);
