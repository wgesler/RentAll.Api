using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace RentAll.Infrastructure.Services;

public static class CreditCardStatementSpreadsheetReader
{
    public static bool IsSpreadsheet(string? fileName, string? contentType)
    {
        var name = (fileName ?? string.Empty).Trim().ToLowerInvariant();
        var type = (contentType ?? string.Empty).Trim().ToLowerInvariant();
        return name.EndsWith(".csv")
            || name.EndsWith(".xlsx")
            || name.EndsWith(".xls")
            || type.Contains("csv")
            || type.Contains("spreadsheet")
            || type.Contains("excel");
    }

    public static bool IsLegacyExcel(string? fileName, string? contentType)
    {
        var name = (fileName ?? string.Empty).Trim().ToLowerInvariant();
        var type = (contentType ?? string.Empty).Trim().ToLowerInvariant();
        return name.EndsWith(".xls") && !name.EndsWith(".xlsx") && !type.Contains("openxml");
    }

    public static IReadOnlyList<IReadOnlyList<string>> ReadTables(byte[] content, string? fileName, string? contentType)
    {
        if (content == null || content.Length == 0)
            return [];

        var name = (fileName ?? string.Empty).Trim().ToLowerInvariant();
        if (name.EndsWith(".xlsx") || (contentType ?? string.Empty).Contains("openxml", StringComparison.OrdinalIgnoreCase))
            return ReadXlsx(content);

        return [ReadCsv(Encoding.UTF8.GetString(content))];
    }

    private static List<string> ReadCsv(string text)
    {
        return (text ?? string.Empty)
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n')
            .Select(line => string.Join('\t', SplitCsvLine(line)))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
    }

    private static List<string> SplitCsvLine(string line)
    {
        var cells = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                cells.Add(current.ToString().Trim());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        cells.Add(current.ToString().Trim());
        return cells;
    }

    private static List<IReadOnlyList<string>> ReadXlsx(byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        var sharedStrings = ReadSharedStrings(archive);
        var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml")
            ?? archive.Entries.FirstOrDefault(entry => entry.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase) && entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
        if (sheetEntry == null)
            return [];

        using var sheetStream = sheetEntry.Open();
        var document = XDocument.Load(sheetStream);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var rows = new List<string>();
        foreach (var row in document.Root?.Descendants(ns + "row") ?? [])
        {
            var cells = new SortedDictionary<int, string>();
            foreach (var cell in row.Elements(ns + "c"))
            {
                var reference = (string?)cell.Attribute("r") ?? string.Empty;
                var columnIndex = GetColumnIndex(reference);
                cells[columnIndex] = ReadCellValue(cell, ns, sharedStrings);
            }

            if (cells.Count == 0)
                continue;

            var maxIndex = cells.Keys.Max();
            var values = Enumerable.Range(0, maxIndex + 1).Select(index => cells.TryGetValue(index, out var value) ? value : string.Empty).ToList();
            rows.Add(string.Join('\t', values));
        }

        return rows.Count == 0 ? [] : [rows];
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry == null)
            return [];

        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        return document.Root?
            .Elements(ns + "si")
            .Select(item => string.Concat(item.Descendants(ns + "t").Select(node => node.Value)))
            .ToList()
            ?? [];
    }

    private static string ReadCellValue(XElement cell, XNamespace ns, IReadOnlyList<string> sharedStrings)
    {
        var type = (string?)cell.Attribute("t");
        var value = cell.Element(ns + "v")?.Value ?? string.Concat(cell.Descendants(ns + "t").Select(node => node.Value));
        if (type == "s" && int.TryParse(value, out var sharedIndex) && sharedIndex >= 0 && sharedIndex < sharedStrings.Count)
            return sharedStrings[sharedIndex]?.Trim() ?? string.Empty;

        return (value ?? string.Empty).Trim();
    }

    private static int GetColumnIndex(string cellReference)
    {
        var letters = Regex.Match(cellReference ?? string.Empty, @"^[A-Za-z]+").Value.ToUpperInvariant();
        var index = 0;
        foreach (var letter in letters)
            index = (index * 26) + (letter - 'A' + 1);

        return Math.Max(0, index - 1);
    }
}
