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
        var type = (contentType ?? string.Empty).Trim();
        if (name.EndsWith(".csv") || type.Contains("csv", StringComparison.OrdinalIgnoreCase))
            return [ReadCsv(DecodeText(content))];

        if (name.EndsWith(".xlsx") || type.Contains("openxml", StringComparison.OrdinalIgnoreCase))
            return ReadXlsx(content);

        return [ReadCsv(DecodeText(content))];
    }

    private static string DecodeText(byte[] content)
    {
        if (content.Length >= 3 && content[0] == 0xEF && content[1] == 0xBB && content[2] == 0xBF)
            return Encoding.UTF8.GetString(content, 3, content.Length - 3);
        if (content.Length >= 2 && content[0] == 0xFF && content[1] == 0xFE)
            return Encoding.Unicode.GetString(content);
        if (content.Length >= 2 && content[0] == 0xFE && content[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(content);
        if (content.Length >= 4 && content[1] == 0 && content[3] == 0)
            return Encoding.Unicode.GetString(content);

        return Encoding.UTF8.GetString(content);
    }

    private static List<string> ReadCsv(string text)
    {
        var normalized = (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
        var delimiter = DetectDelimiter(normalized);
        return normalized
            .Split('\n')
            .Select(line => string.Join('\t', SplitCsvLine(line, delimiter)))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
    }

    private static char DetectDelimiter(string text)
    {
        var sample = text.Split('\n').FirstOrDefault(line => line.IndexOfAny([',', ';', '\t']) >= 0) ?? string.Empty;
        var comma = 0;
        var semicolon = 0;
        var tab = 0;
        var inQuotes = false;
        foreach (var ch in sample)
        {
            if (ch == '"')
                inQuotes = !inQuotes;
            else if (!inQuotes && ch == ',')
                comma++;
            else if (!inQuotes && ch == ';')
                semicolon++;
            else if (!inQuotes && ch == '\t')
                tab++;
        }

        if (tab >= comma && tab >= semicolon && tab > 0)
            return '\t';
        return semicolon > comma ? ';' : ',';
    }

    private static List<string> SplitCsvLine(string line)
        => SplitCsvLine(line, ',');

    private static List<string> SplitCsvLine(string line, char delimiter)
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

            if (ch == delimiter && !inQuotes)
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
