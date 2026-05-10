using ClosedXML.Excel;
using ASVSGuard.Core.Entities;
using ASVSGuard.Core.Interfaces;
using System.Text.RegularExpressions;

namespace ASVSGuard.Infrastructure.Parsers;

public partial class ExcelExigenceParser : IExcelParser
{
    private static readonly string[] SheetNames =
    [
        "Architecture", "Authentication", "Session Management", "Access Control",
        "Validation", "Cryptography", "Error Handling", "Data Protection",
        "Communications", "Malicious Code", "Business Logic", "Files and Resources",
        "API", "Configuration"
    ];

    [GeneratedRegex(@"^\d+\.\d+\.\d+$")]
    private static partial Regex ExigenceCodeRegex();

    [GeneratedRegex(@"\(\[.*?\]\(.*?\)\)")]
    private static partial Regex MarkdownLinkRegex();

    public IEnumerable<Exigence> Parse(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var results = new List<Exigence>();

        foreach (var sheetName in SheetNames)
        {
            if (!workbook.TryGetWorksheet(sheetName, out var sheet))
                continue;

            var category = sheetName;
            var currentGroup = string.Empty;

            foreach (var row in sheet.RowsUsed().Skip(1))
            {
                var codeCell = row.Cell(1).GetString().Trim();
                var descCell = row.Cell(4).GetString().Trim();

                // Section header: starts with "V" and contains "." like "V2.1 Password Security..."
                if (codeCell.StartsWith("V") && codeCell.Contains('.') && !ExigenceCodeRegex().IsMatch(codeCell))
                {
                    currentGroup = codeCell;
                    continue;
                }

                if (!ExigenceCodeRegex().IsMatch(codeCell))
                    continue;

                var levelStr = row.Cell(3).GetString().Trim();
                var cweStr = row.Cell(5).GetString().Trim();
                var statusStr = row.Cell(6).GetString().Trim();
                var notes = row.Cell(7).GetString().Trim();

                _ = int.TryParse(levelStr.Replace(".0", ""), out var level);
                int? cwe = int.TryParse(cweStr.Replace(".0", ""), out var cweVal) ? cweVal : null;

                var description = MarkdownLinkRegex().Replace(descCell, string.Empty).Trim();

                var status = statusStr.ToLowerInvariant() switch
                {
                    "pass" or "compliant" => ExigenceStatus.Compliant,
                    "fail" or "missing" => ExigenceStatus.Missing,
                    _ => ExigenceStatus.Unknown
                };

                results.Add(new Exigence
                {
                    Code = codeCell,
                    Category = category,
                    Group = currentGroup,
                    Level = level,
                    CWE = cwe,
                    Description = description,
                    Status = status,
                    Notes = string.IsNullOrWhiteSpace(notes) ? null : notes
                });
            }
        }

        return results;
    }
}
