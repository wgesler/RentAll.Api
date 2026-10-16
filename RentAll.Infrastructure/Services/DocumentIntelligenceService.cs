using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.Identity;
using ImageMagick;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Enums;
using RentAll.Domain.Models.Contacts;
using RentAll.Domain.Models.Maintenances;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RentAll.Infrastructure.Services;

public class DocumentIntelligenceService : IDocumentIntelligenceService
{
    private const string PrebuiltReceiptModelId = "prebuilt-receipt";
    private const string PrebuiltLayoutModelId = "prebuilt-layout";
    private static readonly Regex PropertyCodePattern = new(@"\bR-\d+(?:-\d+)*\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex CardLastFourPattern = new(
        @"(?:[#*xX\u2022\u00B7\s.-]{4,20})(\d{4})\b"
        + @"|(?:ending|ends)\s+(?:in|with)\s+[#*xX]*(\d{4})"
        + @"|\b(?:visa|master\s*card|mc|discover|disc|amex|american\s*express)\b[^\d]{0,40}(?:[#*xX\s.-]{0,20})(\d{4})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PaymentSectionCardLastFourPattern = new(
        @"\b(?:visa|master\s*card|mc|discover|disc|amex|american\s*express|payment|card(?:\s*(?:no|number|#))?)\b[\s\S]{0,120}?(?:[#*xX\u2022\u00B7\s.-]{3,20})(\d{4})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex MerchantNoiseLinePattern = new(
        @"^(?:table|check|server|cashier|guests?|seat|dine\s*in|order\s*type|auth(?:orization)?\s*code|amount\s*paid|balance\s*due|subtotal|total|tax|visa|master\s*card|discover|amex)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private readonly DocumentIntelligenceSettings _settings;
    private readonly ILogger<DocumentIntelligenceService> _logger;

    public DocumentIntelligenceService(
        IOptions<DocumentIntelligenceSettings> settings,
        ILogger<DocumentIntelligenceService> logger)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger;
    }

    public bool IsEnabled => _settings.Enabled;

    public async Task<ReceiptDocumentExtraction> ExtractReceiptAsync(
        byte[] content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
            throw new InvalidOperationException("Document Intelligence is not enabled.");

        if (content == null || content.Length == 0)
            throw new ArgumentException("Receipt content is required.", nameof(content));

        if (IsTargetLogo(content))
        {
            _logger.LogError("[ReceiptExtractTrace] Step=TargetLogo VendorName=Target");
            return new ReceiptDocumentExtraction { VendorName = "Target" };
        }

        var endpoint = (_settings.Endpoint ?? string.Empty).Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new InvalidOperationException("Document Intelligence endpoint is not configured.");

        var client = new DocumentIntelligenceClient(
            new Uri(endpoint),
            new DefaultAzureCredential());

        _logger.LogError(
            "[ReceiptExtractTrace] Step=Analyze Start ContentType={ContentType} ContentLength={ContentLength}",
            contentType,
            content.Length);

        var operation = await client.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            PrebuiltReceiptModelId,
            BinaryData.FromBytes(content),
            cancellationToken: cancellationToken);

        var analyzeResult = operation.Value;
        var warnings = new List<string>();
        var confidences = new Dictionary<string, double?>(StringComparer.OrdinalIgnoreCase);

        if (analyzeResult.Documents == null || analyzeResult.Documents.Count == 0)
        {
            warnings.Add("No receipt document was detected in the uploaded file.");
            return new ReceiptDocumentExtraction
            {
                Warnings = warnings,
                FieldConfidences = confidences
            };
        }

        var document = analyzeResult.Documents[0];
        var fullContent = analyzeResult.Content ?? string.Empty;
        var merchantName = ReadStringField(document, "MerchantName", confidences);
        if (string.IsNullOrWhiteSpace(merchantName))
            merchantName = InferMerchantNameFromContent(fullContent);
        else
            merchantName = NormalizeMerchantName(merchantName);

        var paymentMethod = ReadStringField(document, "PaymentMethod", confidences);
        var transactionDate = ReadDateField(document, "TransactionDate", confidences);
        var total = ReadCurrencyField(document, "Total", confidences);
        var subtotal = ReadCurrencyField(document, "Subtotal", confidences);
        var tax = ReadCurrencyField(document, "TotalTax", confidences) ?? ReadCurrencyField(document, "Tax", confidences);
        var lineItemDescriptions = ExtractLineItemDescriptions(document);
        var detectedPropertyCodes = ExtractPropertyCodes(fullContent);
        var (cardLastFour, cardTypeId) = ParsePaymentCard(paymentMethod, fullContent);

        var amount = ReceiptAmountParser.ResolveAmount(total, subtotal, tax, fullContent);

        var description = BuildDescription(lineItemDescriptions);

        if (!amount.HasValue)
            warnings.Add("Total amount was not detected. Please enter the amount manually.");

        if (!transactionDate.HasValue)
            warnings.Add("Receipt date was not detected. Please confirm the receipt date.");

        if (string.IsNullOrWhiteSpace(merchantName))
            warnings.Add("Merchant name was not detected. Please select or enter a vendor.");

        if (!string.IsNullOrWhiteSpace(cardLastFour) && !cardTypeId.HasValue)
            warnings.Add($"Card ending {cardLastFour} was detected but card type was unclear.");

        _logger.LogError(
            "[ReceiptExtractTrace] Step=Analyze Complete ReceiptDate={ReceiptDate} Amount={Amount} Total={Total} Subtotal={Subtotal} Tax={Tax} VendorName={VendorName} CardLastFour={CardLastFour} CardTypeId={CardTypeId} PropertyCodes={PropertyCodes} ItemCount={ItemCount}",
            transactionDate,
            amount,
            total,
            subtotal,
            tax,
            merchantName,
            cardLastFour,
            cardTypeId,
            string.Join(",", detectedPropertyCodes),
            lineItemDescriptions.Count);

        return new ReceiptDocumentExtraction
        {
            ReceiptDate = transactionDate,
            Amount = amount,
            VendorName = merchantName,
            Description = description,
            CardLastFour = cardLastFour,
            CardTypeId = cardTypeId,
            FullText = fullContent,
            DetectedPropertyCodes = detectedPropertyCodes,
            LineItemDescriptions = lineItemDescriptions,
            Warnings = warnings,
            FieldConfidences = confidences
        };
    }

    private static string? ReadStringField(
        AnalyzedDocument document,
        string fieldName,
        IDictionary<string, double?> confidences)
    {
        if (!document.Fields.TryGetValue(fieldName, out var field))
            return null;

        confidences[fieldName] = field.Confidence;
        return field.ValueString?.Trim();
    }

    private static DateOnly? ReadDateField(
        AnalyzedDocument document,
        string fieldName,
        IDictionary<string, double?> confidences)
    {
        if (!document.Fields.TryGetValue(fieldName, out var field))
            return null;

        confidences[fieldName] = field.Confidence;

        if (field.ValueDate.HasValue)
            return DateOnly.FromDateTime(field.ValueDate.Value.DateTime);

        if (!string.IsNullOrWhiteSpace(field.Content)
            && DateOnly.TryParse(field.Content, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static decimal? ReadCurrencyField(
        AnalyzedDocument document,
        string fieldName,
        IDictionary<string, double?> confidences)
    {
        if (!document.Fields.TryGetValue(fieldName, out var field))
            return null;

        confidences[fieldName] = field.Confidence;

        if (field.FieldType == DocumentFieldType.Currency && field.ValueCurrency != null)
            return Convert.ToDecimal(field.ValueCurrency.Amount);

        if (field.FieldType == DocumentFieldType.Double)
            return Convert.ToDecimal(field.ValueDouble);

        if (!string.IsNullOrWhiteSpace(field.Content)
            && decimal.TryParse(field.Content, NumberStyles.Currency | NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static IReadOnlyList<string> ExtractLineItemDescriptions(AnalyzedDocument document)
    {
        if (!document.Fields.TryGetValue("Items", out var itemsField)
            || itemsField.FieldType != DocumentFieldType.List)
        {
            return Array.Empty<string>();
        }

        var descriptions = new List<string>();
        foreach (var item in itemsField.ValueList ?? [])
        {
            if (item.FieldType != DocumentFieldType.Dictionary
                || !item.ValueDictionary.TryGetValue("Description", out var itemDescription)
                || itemDescription.FieldType != DocumentFieldType.String)
            {
                continue;
            }

            var itemText = itemDescription.ValueString?.Trim();
            if (!string.IsNullOrWhiteSpace(itemText)
                && !descriptions.Contains(itemText, StringComparer.OrdinalIgnoreCase))
            {
                descriptions.Add(itemText);
            }
        }

        return descriptions;
    }

    private static IReadOnlyList<string> ExtractPropertyCodes(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Array.Empty<string>();

        var matches = PropertyCodePattern.Matches(content);
        if (matches.Count == 0)
            return Array.Empty<string>();

        return matches
            .Select(match => match.Value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(code => code.Length)
            .ToList();
    }

    private static (string? LastFour, int? CardTypeId) ParsePaymentCard(string? paymentMethod, string content)
    {
        var searchText = string.Join("\n", new[] { paymentMethod, content }.Where(value => !string.IsNullOrWhiteSpace(value)));
        if (string.IsNullOrWhiteSpace(searchText))
            return (null, null);

        var cardTypeId = ParseCardType(searchText);
        var lastFour = ExtractCardLastFour(searchText);
        if (string.IsNullOrWhiteSpace(lastFour))
            return (null, cardTypeId);

        return (lastFour, cardTypeId ?? ParseCardType(searchText));
    }

    private static string? ExtractCardLastFour(string searchText)
    {
        foreach (Match match in CardLastFourPattern.Matches(searchText))
        {
            var lastFour = ReadCardLastFourFromMatch(match);
            if (!string.IsNullOrWhiteSpace(lastFour))
                return lastFour;
        }

        var paymentSectionMatch = PaymentSectionCardLastFourPattern.Match(searchText);
        if (paymentSectionMatch.Success)
        {
            var lastFour = ReadCardLastFourFromMatch(paymentSectionMatch);
            if (!string.IsNullOrWhiteSpace(lastFour))
                return lastFour;
        }

        return null;
    }

    private static string? ReadCardLastFourFromMatch(Match match)
    {
        return match.Groups.Cast<Group>()
            .Skip(1)
            .Select(group => group.Value)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?.Trim();
    }

    private static bool IsTargetLogo(byte[] content)
    {
        try
        {
            using var image = new MagickImage(content);
            if (image.Width < 16 || image.Height < 16)
                return false;

            image.AutoOrient();
            image.Alpha(AlphaOption.Remove);
            image.ColorType = ColorType.TrueColor;
            image.Resize(64, 64);

            var pixels = image.GetPixels();
            var width = image.Width;
            var height = image.Height;
            var cx = (width - 1) / 2.0;
            var cy = (height - 1) / 2.0;
            var maxRadius = Math.Min(cx, cy);
            if (maxRadius < 8)
                return false;

            var red = 0;
            var white = 0;
            var other = 0;
            var bandRed = new int[16];
            var bandWhite = new int[16];
            var bandTotal = new int[16];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var color = pixels.GetPixel(x, y).ToColor();
                    if (color == null)
                        continue;

                    var kind = TargetLogoPixelKind(color.R, color.G, color.B);
                    if (kind == 1)
                        red++;
                    else if (kind == 2)
                        white++;
                    else
                        other++;

                    var dx = x - cx;
                    var dy = y - cy;
                    var distance = Math.Sqrt((dx * dx) + (dy * dy)) / maxRadius;
                    if (distance > 1)
                        continue;

                    var band = Math.Min(15, (int)(distance * 16));
                    bandTotal[band]++;
                    if (kind == 1)
                        bandRed[band]++;
                    else if (kind == 2)
                        bandWhite[band]++;
                }
            }

            var total = red + white + other;
            if (total == 0 || other / (double)total > 0.18 || red / (double)total < 0.08 || white / (double)total < 0.08)
                return false;

            var rings = new List<char>();
            for (var band = 0; band < bandTotal.Length; band++)
            {
                if (bandTotal[band] < 8)
                    continue;

                char? ring = null;
                if (bandRed[band] / (double)bandTotal[band] >= 0.55)
                    ring = 'R';
                else if (bandWhite[band] / (double)bandTotal[band] >= 0.55)
                    ring = 'W';

                if (ring.HasValue && (rings.Count == 0 || rings[^1] != ring.Value))
                    rings.Add(ring.Value);
            }

            var pattern = new string(rings.ToArray());
            return pattern.Contains("RWR", StringComparison.Ordinal);
        }
        catch (MagickException)
        {
            return false;
        }
    }

    private static int TargetLogoPixelKind(byte red, byte green, byte blue)
    {
        if (red >= 150 && green <= 90 && blue <= 90 && red >= green + 70 && red >= blue + 70)
            return 1;

        if (red >= 210 && green >= 210 && blue >= 210)
            return 2;

        return 0;
    }

    private static string? InferMerchantNameFromContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        var lines = content
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Take(6)
            .ToList();

        if (lines.Count == 0)
            return null;

        var merchantLines = lines.Where(LooksLikeMerchantLine).Take(2).ToList();
        if (merchantLines.Count == 0)
            return NormalizeMerchantName(lines[0]);

        return NormalizeMerchantName(string.Join(" ", merchantLines));
    }

    private static bool LooksLikeMerchantLine(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length < 2 || trimmed.Length > 80)
            return false;

        if (!Regex.IsMatch(trimmed, @"[A-Za-z]"))
            return false;

        if (Regex.IsMatch(trimmed, @"^\d"))
            return false;

        if (Regex.IsMatch(trimmed, @"^\d{1,2}/\d{1,2}/\d{2,4}\b"))
            return false;

        if (Regex.IsMatch(trimmed, @"^\d{1,2}:\d{2}\b"))
            return false;

        if (MerchantNoiseLinePattern.IsMatch(trimmed))
            return false;

        if (Regex.IsMatch(trimmed, @"\b(?:st|street|ave|avenue|road|rd|blvd|suite|ste|zip)\b", RegexOptions.IgnoreCase))
            return false;

        return true;
    }

    private static string? NormalizeMerchantName(string? merchantName)
    {
        if (string.IsNullOrWhiteSpace(merchantName))
            return null;

        var normalized = Regex.Replace(merchantName.Trim(), @"\s+", " ");
        var firstLine = normalized
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        return string.IsNullOrWhiteSpace(firstLine) ? normalized : firstLine;
    }

    private static int? ParseCardType(string text)
    {
        if (Regex.IsMatch(text, @"\bvisa\b", RegexOptions.IgnoreCase))
            return (int)CardType.Visa;

        if (Regex.IsMatch(text, @"\b(?:master\s*card|mastercard|\bmc\b)\b", RegexOptions.IgnoreCase))
            return (int)CardType.MasterCard;

        if (Regex.IsMatch(text, @"\b(?:discover|\bdisc\b)\b", RegexOptions.IgnoreCase))
            return (int)CardType.Discover;

        if (Regex.IsMatch(text, @"\b(?:amex|american\s*express)\b", RegexOptions.IgnoreCase))
            return (int)CardType.AmericanExpress;

        return null;
    }

    private static string? BuildDescription(IReadOnlyList<string> lineItemDescriptions)
    {
        var items = lineItemDescriptions
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return items.Count == 0 ? null : string.Join("; ", items);
    }

    public async Task<W9DocumentExtraction> ExtractW9Async(
        byte[] content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
            throw new InvalidOperationException("Document Intelligence is not enabled.");

        if (content == null || content.Length == 0)
            throw new ArgumentException("W-9 content is required.", nameof(content));

        var endpoint = (_settings.Endpoint ?? string.Empty).Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new InvalidOperationException("Document Intelligence endpoint is not configured.");

        var client = new DocumentIntelligenceClient(new Uri(endpoint), new DefaultAzureCredential());
        var options = new AnalyzeDocumentOptions(PrebuiltLayoutModelId, BinaryData.FromBytes(content));
        options.Features.Add(DocumentAnalysisFeature.QueryFields);
        options.Features.Add(DocumentAnalysisFeature.KeyValuePairs);
        options.Features.Add(DocumentAnalysisFeature.OcrHighResolution);
        foreach (var fieldName in W9FormParser.QueryFields)
            options.QueryFields.Add(fieldName);

        _logger.LogError(
            "[W9ExtractTrace] Step=Analyze Start ContentType={ContentType} ContentLength={ContentLength}",
            contentType,
            content.Length);

        var operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, options, cancellationToken);
        var fields = ReadW9QueryFields(operation.Value);
        AddW9KeyValueFields(operation.Value, fields);
        var extraction = W9FormParser.Parse(fields, BuildW9Text(operation.Value));
        _logger.LogError(
            "[W9ExtractTrace] Step=Analyze Complete TaxClassificationId={TaxClassificationId} OwnerIdTypeId={OwnerIdTypeId} HasTin={HasTin} WarningCount={WarningCount} FieldKeys={FieldKeys} ContentLength={ContentLength} HasSelectedMark={HasSelectedMark}",
            extraction.FederalTaxClassificationId,
            extraction.OwnerIdTypeId,
            !string.IsNullOrWhiteSpace(extraction.IdentificationNumber),
            extraction.Warnings.Count,
            string.Join(",", fields.Keys),
            operation.Value.Content?.Length ?? 0,
            (operation.Value.Content ?? string.Empty).Contains(":selected:", StringComparison.OrdinalIgnoreCase));
        return extraction;
    }

    private static Dictionary<string, string> ReadW9QueryFields(AnalyzeResult analyzeResult)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var document in analyzeResult.Documents ?? [])
        {
            foreach (var pair in document.Fields)
                AddW9Field(fields, pair.Key, pair.Value);
        }

        return fields;
    }

    private static void AddW9KeyValueFields(AnalyzeResult analyzeResult, Dictionary<string, string> fields)
    {
        foreach (var pair in analyzeResult.KeyValuePairs ?? [])
        {
            var key = pair.Key?.Content ?? string.Empty;
            var value = pair.Value?.Content ?? string.Empty;
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                continue;

            string? target = null;
            if (key.Contains("social security", StringComparison.OrdinalIgnoreCase))
                target = W9FormParser.SocialSecurityNumber;
            else if (key.Contains("employer", StringComparison.OrdinalIgnoreCase) && key.Contains("identification", StringComparison.OrdinalIgnoreCase))
                target = W9FormParser.EmployerIdentificationNumber;
            else if (key.Contains("tax classification", StringComparison.OrdinalIgnoreCase) || key.Contains("federal tax", StringComparison.OrdinalIgnoreCase))
                target = W9FormParser.FederalTaxClassification;

            if (target != null && !fields.ContainsKey(target))
                fields[target] = value.Trim();
        }
    }

    private static string BuildW9Text(AnalyzeResult analyzeResult)
    {
        var text = new System.Text.StringBuilder();
        if (!string.IsNullOrWhiteSpace(analyzeResult.Content))
            text.AppendLine(analyzeResult.Content);

        foreach (var page in analyzeResult.Pages ?? [])
        {
            var words = (page.Words ?? [])
                .Select(word => (Content: word.Content ?? string.Empty, Center: PolygonCenter(word.Polygon)))
                .Where(word => word.Content.Length > 0)
                .ToList();

            foreach (var mark in page.SelectionMarks ?? [])
            {
                if (mark.State != DocumentSelectionMarkState.Selected)
                    continue;

                var markCenter = PolygonCenter(mark.Polygon);
                var label = string.Join(' ', words
                    .Where(word => Math.Abs(word.Center.Y - markCenter.Y) < 0.35 && word.Center.X > markCenter.X && word.Center.X - markCenter.X < 5)
                    .OrderBy(word => word.Center.X)
                    .Take(12)
                    .Select(word => word.Content));
                if (!string.IsNullOrWhiteSpace(label))
                    text.AppendLine(":selected: " + label);
            }
        }

        foreach (var pair in analyzeResult.KeyValuePairs ?? [])
        {
            var key = pair.Key?.Content;
            var value = pair.Value?.Content;
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                text.AppendLine(key + " " + value);
        }

        return text.ToString();
    }

    private static (float X, float Y) PolygonCenter(IReadOnlyList<float>? polygon)
    {
        if (polygon == null || polygon.Count < 2)
            return (0, 0);

        float sumX = 0;
        float sumY = 0;
        var points = 0;
        for (var index = 0; index + 1 < polygon.Count; index += 2)
        {
            sumX += polygon[index];
            sumY += polygon[index + 1];
            points++;
        }

        return points == 0 ? (0, 0) : (sumX / points, sumY / points);
    }

    private static void AddW9Field(Dictionary<string, string> fields, string key, DocumentField field)
    {
        var text = ReadW9FieldText(field);
        if (!string.IsNullOrWhiteSpace(text))
            fields[key] = text;
    }

    private static string? ReadW9FieldText(DocumentField field)
    {
        if (!string.IsNullOrWhiteSpace(field.ValueString))
            return field.ValueString.Trim();

        if (field.ValueSelectionGroup is { Count: > 0 })
        {
            var selected = string.Join(" ", field.ValueSelectionGroup.Where(value => !string.IsNullOrWhiteSpace(value)));
            if (!string.IsNullOrWhiteSpace(selected))
                return selected.Trim();
        }

        return string.IsNullOrWhiteSpace(field.Content) ? null : field.Content.Trim();
    }

    public async Task<CreditCardStatementExtraction> ExtractCreditCardStatementAsync(byte[] content, string contentType, string? fileName, CancellationToken cancellationToken = default)
    {
        if (content == null || content.Length == 0)
            throw new ArgumentException("Statement content is required.", nameof(content));

        if (CreditCardStatementSpreadsheetReader.IsLegacyExcel(fileName, contentType))
        {
            return new CreditCardStatementExtraction
            {
                Warnings = ["Legacy .xls files are not supported. Save the statement as .xlsx or .csv and upload again."]
            };
        }

        if (CreditCardStatementSpreadsheetReader.IsSpreadsheet(fileName, contentType))
        {
            var spreadsheetTables = CreditCardStatementSpreadsheetReader.ReadTables(content, fileName, contentType);
            var spreadsheetText = string.Join('\n', spreadsheetTables.SelectMany(table => table));
            return CreditCardStatementLineParser.ParseTables(spreadsheetTables, spreadsheetText);
        }

        if (CreditCardStatementPdfReader.IsPdf(fileName, contentType, content))
        {
            var pdfText = CreditCardStatementPdfReader.ReadLines(content);
            var pdfExtraction = CreditCardStatementLineParser.ParseTables([], pdfText);
            _logger.LogError("[CreditReportTrace] Step=PdfText LineCount={LineCount} TextLength={TextLength}", pdfExtraction.Lines.Count, pdfText.Length);
            return pdfExtraction;
        }

        if (!_settings.Enabled)
            throw new InvalidOperationException("Document Intelligence is not enabled.");

        var endpoint = (_settings.Endpoint ?? string.Empty).Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new InvalidOperationException("Document Intelligence endpoint is not configured.");

        var client = new DocumentIntelligenceClient(new Uri(endpoint), new DefaultAzureCredential());
        _logger.LogError("[CreditReportTrace] Step=Analyze Start ContentType={ContentType} FileName={FileName} ContentLength={ContentLength}", contentType, fileName, content.Length);

        var operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, PrebuiltLayoutModelId, BinaryData.FromBytes(content), cancellationToken: cancellationToken);
        var analyzeResult = operation.Value;
        var fullText = analyzeResult.Content ?? string.Empty;
        var tables = BuildTablesFromAnalyzeResult(analyzeResult);
        var extraction = CreditCardStatementLineParser.ParseTables(tables, fullText);
        _logger.LogError("[CreditReportTrace] Step=Analyze Complete LineCount={LineCount} CardLastFour={CardLastFour} WarningCount={WarningCount}", extraction.Lines.Count, extraction.StatementCardLastFour, extraction.Warnings.Count);
        return extraction;
    }

    private static List<IReadOnlyList<string>> BuildTablesFromAnalyzeResult(AnalyzeResult analyzeResult)
    {
        var tables = new List<IReadOnlyList<string>>();
        foreach (var table in analyzeResult.Tables ?? [])
        {
            var rowCount = table.RowCount;
            var columnCount = table.ColumnCount;
            if (rowCount <= 0 || columnCount <= 0)
                continue;

            var grid = Enumerable.Range(0, rowCount).Select(_ => Enumerable.Repeat(string.Empty, columnCount).ToArray()).ToList();
            foreach (var cell in table.Cells ?? [])
            {
                if (cell.RowIndex < 0 || cell.RowIndex >= rowCount || cell.ColumnIndex < 0 || cell.ColumnIndex >= columnCount)
                    continue;

                var existing = grid[cell.RowIndex][cell.ColumnIndex];
                var next = (cell.Content ?? string.Empty).Trim();
                grid[cell.RowIndex][cell.ColumnIndex] = string.IsNullOrWhiteSpace(existing) ? next : $"{existing} {next}";
            }

            tables.Add(grid.Select(row => string.Join('\t', row)).ToList());
        }

        return tables;
    }
}
