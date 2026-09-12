using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Enums;
using RentAll.Domain.Models.Maintenances;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RentAll.Infrastructure.Services;

public class DocumentIntelligenceService : IDocumentIntelligenceService
{
    private const string PrebuiltReceiptModelId = "prebuilt-receipt";
    private static readonly Regex PropertyCodePattern = new(@"\bR-\d+(?:-\d+)*\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex CardLastFourPattern = new(@"(?:\*{2,4}|x{2,4}|#{2,4}|\.{2,4})[\s-]*(\d{4})|(?:ending|ends)\s+(?:in|with)\s+[#*x]*(\d{4})|\b(?:visa|master\s*card|mc|discover|disc|amex|american\s*express)\b[^\d]{0,20}(\d{4})\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex StandaloneLastFourPattern = new(@"\b(\d{4})\b", RegexOptions.Compiled);
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
        var merchantName = ReadStringField(document, "MerchantName", confidences);
        var paymentMethod = ReadStringField(document, "PaymentMethod", confidences);
        var transactionDate = ReadDateField(document, "TransactionDate", confidences);
        var total = ReadCurrencyField(document, "Total", confidences);
        var subtotal = ReadCurrencyField(document, "Subtotal", confidences);
        var tax = ReadCurrencyField(document, "Tax", confidences);
        var fullContent = analyzeResult.Content ?? string.Empty;
        var lineItemDescriptions = ExtractLineItemDescriptions(document);
        var detectedPropertyCodes = ExtractPropertyCodes(fullContent);
        var (cardLastFour, cardTypeId) = ParsePaymentCard(paymentMethod, fullContent);

        var amount = total ?? subtotal;
        if (!amount.HasValue && tax.HasValue && subtotal.HasValue)
            amount = subtotal.Value + tax.Value;

        var description = BuildDescription(merchantName, lineItemDescriptions);

        if (!amount.HasValue)
            warnings.Add("Total amount was not detected. Please enter the amount manually.");

        if (!transactionDate.HasValue)
            warnings.Add("Receipt date was not detected. Please confirm the receipt date.");

        if (string.IsNullOrWhiteSpace(merchantName))
            warnings.Add("Merchant name was not detected. Please select or enter a vendor.");

        if (!string.IsNullOrWhiteSpace(cardLastFour) && !cardTypeId.HasValue)
            warnings.Add($"Card ending {cardLastFour} was detected but card type was unclear.");

        _logger.LogError(
            "[ReceiptExtractTrace] Step=Analyze Complete ReceiptDate={ReceiptDate} Amount={Amount} VendorName={VendorName} CardLastFour={CardLastFour} CardTypeId={CardTypeId} PropertyCodes={PropertyCodes} ItemCount={ItemCount}",
            transactionDate,
            amount,
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
            .Select(match => match.Value.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(code => code.Length)
            .ToList();
    }

    private static (string? LastFour, int? CardTypeId) ParsePaymentCard(string? paymentMethod, string content)
    {
        var searchText = string.Join(" ", new[] { paymentMethod, content }.Where(value => !string.IsNullOrWhiteSpace(value)));
        if (string.IsNullOrWhiteSpace(searchText))
            return (null, null);

        string? lastFour = null;
        foreach (Match match in CardLastFourPattern.Matches(searchText))
        {
            lastFour = match.Groups.Cast<Group>()
                .Skip(1)
                .Select(group => group.Value)
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
                ?.Trim();

            if (!string.IsNullOrWhiteSpace(lastFour))
                break;
        }

        if (string.IsNullOrWhiteSpace(lastFour))
        {
            var cardTypeOnly = ParseCardType(searchText);
            if (cardTypeOnly.HasValue)
            {
                foreach (Match match in StandaloneLastFourPattern.Matches(searchText))
                {
                    var candidate = match.Groups[1].Value;
                    if (candidate.Length == 4)
                    {
                        lastFour = candidate;
                        break;
                    }
                }
            }
        }

        if (string.IsNullOrWhiteSpace(lastFour))
            return (null, null);

        return (lastFour, ParseCardType(searchText));
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

    private static string? BuildDescription(string? merchantName, IReadOnlyList<string> lineItemDescriptions)
    {
        var merchant = merchantName?.Trim();
        var items = lineItemDescriptions
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Where(item => !string.Equals(item, merchant, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (string.IsNullOrWhiteSpace(merchant) && items.Count == 0)
            return null;

        if (string.IsNullOrWhiteSpace(merchant))
            return string.Join("; ", items);

        if (items.Count == 0)
            return merchant;

        return $"{merchant} — {string.Join("; ", items)}";
    }
}
