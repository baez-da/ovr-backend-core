using System.Text.RegularExpressions;
using ClosedXML.Excel;
using OVR.Modules.CommonCodes.Persistence;

namespace OVR.Modules.CommonCodes.Features.ImportFromExcel;

public sealed record ExcelParseResult(
    List<ParsedSheet> Sheets,
    List<string> SkippedSheets);

public sealed record ParsedSheet(
    string Type,
    List<CommonCodeDocument> Documents,
    List<ParseError> Errors);

public sealed record ParseError(int Row, string Column, string Error);

public static partial class ExcelParser
{
    private static readonly HashSet<string> MetadataSheets = new(StringComparer.OrdinalIgnoreCase)
    {
        "Cover",
        "Document Control",
        "Change Log Detail"
    };

    private static readonly string[] CodeColumnNames = ["Id", "Code", "Venue Code"];
    private static readonly string[] OrderColumnNames = ["order", "Eventorder"];

    [GeneratedRegex(@"^([A-Z]{2,3})\s+(Description|Long Description|longdescription|Short Description|shortdescription)$", RegexOptions.None)]
    private static partial Regex LanguageColumnRegex();

    public static ExcelParseResult Parse(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);

        var sheets = new List<ParsedSheet>();
        var skippedSheets = new List<string>();

        foreach (var worksheet in workbook.Worksheets)
        {
            var sheetName = worksheet.Name.Trim();

            if (MetadataSheets.Contains(sheetName))
            {
                skippedSheets.Add(sheetName);
                continue;
            }

            if (worksheet.LastRowUsed() is null)
                continue;

            sheets.Add(ParseSheet(worksheet, sheetName));
        }

        return new ExcelParseResult(sheets, skippedSheets);
    }

    private static ParsedSheet ParseSheet(IXLWorksheet worksheet, string sheetName)
    {
        var type = sheetName.Trim().ToUpperInvariant();
        var errors = new List<ParseError>();
        var documents = new List<CommonCodeDocument>();

        var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        if (lastCol == 0)
            return new ParsedSheet(type, documents, errors);

        // Read headers
        var headers = new Dictionary<int, string>();
        for (var col = 1; col <= lastCol; col++)
        {
            var header = worksheet.Cell(1, col).GetString().Trim();
            if (!string.IsNullOrEmpty(header))
                headers[col] = header;
        }

        // Find code column
        int? codeCol = null;
        foreach (var name in CodeColumnNames)
        {
            var match = headers.FirstOrDefault(h =>
                string.Equals(h.Value, name, StringComparison.OrdinalIgnoreCase));
            if (match.Value is not null)
            {
                codeCol = match.Key;
                break;
            }
        }

        if (codeCol is null)
        {
            errors.Add(new ParseError(1, "", "No code column found (expected Id, Code, or Venue Code)"));
            return new ParsedSheet(type, documents, errors);
        }

        // Find order column
        int? orderCol = null;
        foreach (var name in OrderColumnNames)
        {
            var match = headers.FirstOrDefault(h =>
                string.Equals(h.Value, name, StringComparison.OrdinalIgnoreCase));
            if (match.Value is not null)
            {
                orderCol = match.Key;
                break;
            }
        }

        // Classify language columns
        // Key: (lang, variant) where variant is "long" or "short"
        var langColumns = new Dictionary<int, (string Lang, string Variant)>();
        // Track "Description" columns that might be overridden by "Long Description"
        var descriptionCols = new Dictionary<string, int>(); // lang -> col for plain "Description"
        var longDescriptionCols = new Dictionary<string, int>(); // lang -> col for explicit "Long Description"

        foreach (var (col, header) in headers)
        {
            var regex = LanguageColumnRegex();
            var m = regex.Match(header);
            if (!m.Success) continue;

            var lang = m.Groups[1].Value.ToLowerInvariant();
            var descType = m.Groups[2].Value;

            var variant = descType switch
            {
                "Short Description" or "shortdescription" => "short",
                _ => "long"
            };

            langColumns[col] = (lang, variant);

            if (variant == "long")
            {
                if (descType is "Long Description" or "longdescription")
                    longDescriptionCols[lang] = col;
                else // plain "Description"
                    descriptionCols[lang] = col;
            }
        }

        // If both Description and Long Description exist for the same lang,
        // Long Description takes priority as "long"; demote plain Description to attribute
        var demotedCols = new HashSet<int>();
        foreach (var (lang, descCol) in descriptionCols)
        {
            if (longDescriptionCols.ContainsKey(lang))
                demotedCols.Add(descCol);
        }

        // Determine attribute columns: everything that's not code, order, or (active) language
        var specialCols = new HashSet<int>();
        specialCols.Add(codeCol.Value);
        if (orderCol.HasValue) specialCols.Add(orderCol.Value);
        foreach (var col in langColumns.Keys)
        {
            if (!demotedCols.Contains(col))
                specialCols.Add(col);
        }

        // Demoted description cols are discarded (not attributes, not language)
        foreach (var col in demotedCols)
            specialCols.Add(col);

        var attributeCols = headers.Keys.Where(c => !specialCols.Contains(c)).ToList();

        // Parse rows
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        var seenCodes = new HashSet<string>(StringComparer.Ordinal);
        var rowPosition = 0;

        for (var row = 2; row <= lastRow; row++)
        {
            var code = worksheet.Cell(row, codeCol.Value).GetString().Trim();

            if (string.IsNullOrEmpty(code))
            {
                // Check if entire row is empty
                var hasAnyValue = false;
                for (var col = 1; col <= lastCol; col++)
                {
                    if (!string.IsNullOrWhiteSpace(worksheet.Cell(row, col).GetString()))
                    {
                        hasAnyValue = true;
                        break;
                    }
                }

                if (hasAnyValue)
                {
                    errors.Add(new ParseError(row, headers[codeCol.Value], "Empty code"));
                }

                continue;
            }

            if (!seenCodes.Add(code))
            {
                errors.Add(new ParseError(row, headers[codeCol.Value], $"Duplicate code: {code}"));
                continue;
            }

            rowPosition++;

            var order = orderCol.HasValue
                ? ParseOrder(worksheet.Cell(row, orderCol.Value).GetString(), rowPosition)
                : rowPosition;

            // Build localized names
            var names = new Dictionary<string, LocalizedTextDocument>();
            foreach (var (col, (lang, variant)) in langColumns)
            {
                if (demotedCols.Contains(col))
                    continue;

                var value = worksheet.Cell(row, col).GetString().Trim();
                if (string.IsNullOrEmpty(value))
                    continue;

                if (!names.TryGetValue(lang, out var localized))
                {
                    localized = new LocalizedTextDocument();
                    names[lang] = localized;
                }

                if (variant == "short")
                    localized.Short = value;
                else
                    localized.Long = value;
            }

            // Build attributes
            var attributes = new Dictionary<string, string>();
            foreach (var col in attributeCols)
            {
                var value = worksheet.Cell(row, col).GetString().Trim();
                if (!string.IsNullOrEmpty(value))
                    attributes[headers[col]] = value;
            }

            documents.Add(new CommonCodeDocument
            {
                Id = $"{type}:{code}",
                Type = type,
                Code = code,
                Order = order,
                Name = names,
                Attributes = attributes
            });
        }

        return new ParsedSheet(type, documents, errors);
    }

    private static int ParseOrder(string value, int fallback)
    {
        return int.TryParse(value.Trim(), out var order) ? order : fallback;
    }
}
