using RentAll.Domain.Constants;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Domain.Models.Maintenances;
using System.Text.RegularExpressions;

namespace RentAll.Api.Services;

public class ReceiptExtractEnrichmentService
{
    private readonly IAccountingRepository _accountingRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IPropertyRepository _propertyRepository;

    public ReceiptExtractEnrichmentService(
        IAccountingRepository accountingRepository,
        IOrganizationRepository organizationRepository,
        IPropertyRepository propertyRepository)
    {
        _accountingRepository = accountingRepository;
        _organizationRepository = organizationRepository;
        _propertyRepository = propertyRepository;
    }

    public async Task<ReceiptExtractEnrichmentResult> EnrichAsync(
        ReceiptDocumentExtraction extraction,
        Guid organizationId,
        int? officeId,
        CancellationToken cancellationToken = default)
    {
        var warnings = extraction.Warnings.ToList();
        var detectedPropertyCodes = extraction.DetectedPropertyCodes.ToList();
        var cardPaymentDetected = !string.IsNullOrWhiteSpace(extraction.CardLastFour) || extraction.CardTypeId.HasValue;

        var codesFoundInText = await FindPropertyCodesInReceiptTextAsync(
            extraction.FullText,
            organizationId,
            cancellationToken);
        foreach (var code in codesFoundInText)
        {
            if (!detectedPropertyCodes.Contains(code, StringComparer.OrdinalIgnoreCase))
                detectedPropertyCodes.Add(code);
        }

        var (propertyIds, propertyOfficeId) = await ResolveMatchedPropertiesAsync(
            detectedPropertyCodes,
            organizationId,
            warnings,
            cancellationToken);

        if (propertyIds.Count == 0)
            propertyIds.Add(ReceiptPropertyConstants.CompanyPropertyId.ToString());

        if (detectedPropertyCodes.Count > 0 && propertyIds.Count == 1
            && Guid.TryParse(propertyIds[0], out var solePropertyId)
            && ReceiptPropertyConstants.IsCompanyPropertyId(solePropertyId))
        {
            warnings.Add($"Property code(s) {string.Join(", ", detectedPropertyCodes)} were not found.");
        }
        else if (propertyIds.Count > 1)
        {
            warnings.Add("Multiple property codes were detected on the receipt. Review the selected properties.");
        }

        var bankCardOfficeId = propertyOfficeId ?? officeId;
        var bankCardId = await ResolveBankCardIdAsync(
            extraction,
            organizationId,
            bankCardOfficeId,
            propertyOfficeId,
            warnings);

        return new ReceiptExtractEnrichmentResult
        {
            BankCardId = bankCardId,
            PropertyIds = propertyIds,
            PropertyOfficeId = propertyOfficeId,
            DetectedPropertyCodes = detectedPropertyCodes,
            CardPaymentDetected = cardPaymentDetected,
            Warnings = warnings
        };
    }

    private async Task<(List<string> PropertyIds, int? PropertyOfficeId)> ResolveMatchedPropertiesAsync(
        IReadOnlyList<string> detectedPropertyCodes,
        Guid organizationId,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var propertyIds = new List<string>();
        var propertyOfficeIds = new HashSet<int>();

        foreach (var code in detectedPropertyCodes.OrderByDescending(code => code.Length))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var property = await _propertyRepository.GetPropertyByCodeAsync(code, organizationId);
            if (property == null || property.PropertyId == Guid.Empty)
                continue;

            var propertyId = property.PropertyId.ToString();
            if (!propertyIds.Contains(propertyId, StringComparer.OrdinalIgnoreCase))
                propertyIds.Add(propertyId);

            if (property.OfficeId > 0)
                propertyOfficeIds.Add(property.OfficeId);
        }

        if (propertyOfficeIds.Count > 1)
            warnings.Add("Detected properties belong to different offices. Using the first property office for card matching.");

        int? propertyOfficeId = propertyOfficeIds.Count > 0 ? propertyOfficeIds.First() : null;
        return (propertyIds, propertyOfficeId);
    }

    private async Task<int?> ResolveBankCardIdAsync(
        ReceiptDocumentExtraction extraction,
        Guid organizationId,
        int? bankCardOfficeId,
        int? propertyOfficeId,
        List<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(extraction.CardLastFour) && !extraction.CardTypeId.HasValue)
            return null;

        if (!bankCardOfficeId.HasValue || bankCardOfficeId.Value <= 0)
        {
            if (!string.IsNullOrWhiteSpace(extraction.CardLastFour))
                warnings.Add("Credit card ending detected but a property (or office) is required to match a bank card.");

            return null;
        }

        var propertyOfficeCards = (await _accountingRepository.GetBankCardsByOfficeIdAsync(organizationId, bankCardOfficeId.Value)).ToList();
        var propertyOfficeMatch = TryResolveBankCardMatch(extraction, propertyOfficeCards, out var propertyOfficeWarning);
        if (propertyOfficeMatch.HasValue)
            return propertyOfficeMatch.Value;

        var organizationCards = await LoadOrganizationBankCardsAsync(organizationId);
        if (organizationCards.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(propertyOfficeWarning))
                warnings.Add(propertyOfficeWarning);

            warnings.Add("No bank cards are configured for this organization.");
            return null;
        }

        var organizationMatch = TryResolveBankCardMatch(extraction, organizationCards, out var organizationWarning);
        if (organizationMatch.HasValue)
        {
            var matchedCard = organizationCards.First(card => card.BankCardId == organizationMatch.Value);
            if (propertyOfficeId.HasValue && matchedCard.OfficeId != propertyOfficeId.Value)
                warnings.Add("Matched bank card belongs to a different office; cross-office card accounting will apply on save.");

            if (!string.IsNullOrWhiteSpace(organizationWarning))
                warnings.Add(organizationWarning);

            return organizationMatch.Value;
        }

        if (!string.IsNullOrWhiteSpace(propertyOfficeWarning))
            warnings.Add(propertyOfficeWarning);

        if (!string.IsNullOrWhiteSpace(organizationWarning))
            warnings.Add(organizationWarning);

        return null;
    }

    private async Task<List<BankCard>> LoadOrganizationBankCardsAsync(Guid organizationId)
    {
        var offices = (await _organizationRepository.GetOfficesByOrganizationIdAsync(organizationId)).ToList();
        if (offices.Count == 0)
            return [];

        var officeIds = string.Join(',', offices.Select(office => office.OfficeId));
        return await _accountingRepository.GetBankCardsByOfficeIdsAsync(organizationId, officeIds);
    }

    private static int? TryResolveBankCardMatch(
        ReceiptDocumentExtraction extraction,
        IReadOnlyList<BankCard> cards,
        out string? warning)
    {
        warning = null;
        if (cards.Count == 0)
            return null;

        if (!string.IsNullOrWhiteSpace(extraction.CardLastFour))
        {
            var lastFourMatches = FilterBankCardsByLastFour(cards, extraction.CardLastFour, extraction.CardTypeId);
            if (lastFourMatches.Count == 1)
                return lastFourMatches[0].BankCardId;

            if (lastFourMatches.Count > 1)
            {
                warning = $"Multiple bank cards match ending {extraction.CardLastFour}. Select the card manually.";
                return null;
            }

            var typeFallback = TrySelectFirstBankCardByType(extraction.CardTypeId, cards, out var typeFallbackWarning);
            if (typeFallback.HasValue)
            {
                warning = $"Card ending {extraction.CardLastFour} was not found; {typeFallbackWarning}";
                return typeFallback.Value;
            }

            var cardTypeHint = extraction.CardTypeId.HasValue ? $" {DescribeCardType(extraction.CardTypeId.Value)}" : string.Empty;
            warning = $"No bank card found for ending {extraction.CardLastFour}{cardTypeHint}.";
            return null;
        }

        if (extraction.CardTypeId.HasValue)
        {
            var typeFallback = TrySelectFirstBankCardByType(extraction.CardTypeId, cards, out var typeFallbackWarning);
            if (typeFallback.HasValue)
            {
                warning = typeFallbackWarning;
                return typeFallback.Value;
            }
        }

        return null;
    }

    private async Task<IReadOnlyList<string>> FindPropertyCodesInReceiptTextAsync(
        string fullText,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fullText))
            return Array.Empty<string>();

        cancellationToken.ThrowIfCancellationRequested();
        var offices = (await _organizationRepository.GetOfficesByOrganizationIdAsync(organizationId)).ToList();
        if (offices.Count == 0)
            return Array.Empty<string>();

        var officeIds = string.Join(',', offices.Select(office => office.OfficeId));
        var orgPropertyCodes = (await _propertyRepository.GetPropertyActiveCodesByOfficeIdsAsync(organizationId, officeIds)).ToList();
        if (orgPropertyCodes.Count == 0)
            return Array.Empty<string>();

        return FindPropertyCodesInText(fullText, orgPropertyCodes);
    }

    internal static IReadOnlyList<string> FindPropertyCodesInText(string content, IEnumerable<PropertyCodes> orgPropertyCodes)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Array.Empty<string>();

        var found = new List<string>();
        foreach (var propertyCode in orgPropertyCodes
            .Select(code => (code.PropertyCode ?? string.Empty).Trim())
            .Where(code => code.Length >= 3)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(code => code.Length))
        {
            var pattern = $@"\b{Regex.Escape(propertyCode)}\b";
            if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase)
                && !found.Contains(propertyCode, StringComparer.OrdinalIgnoreCase))
            {
                found.Add(propertyCode);
            }
        }

        return found;
    }

    private static int? TrySelectFirstBankCardByType(int? cardTypeId, IReadOnlyList<BankCard> cards, out string? warning)
    {
        warning = null;
        if (!cardTypeId.HasValue)
            return null;

        var typeMatches = cards.Where(card => card.CardTypeId == cardTypeId.Value).ToList();
        if (typeMatches.Count == 0)
            return null;

        warning = typeMatches.Count == 1
            ? $"selected the only matching {DescribeCardType(cardTypeId.Value)} card."
            : $"selected the first matching {DescribeCardType(cardTypeId.Value)} card.";

        return typeMatches[0].BankCardId;
    }

    private static List<BankCard> FilterBankCardsByLastFour(IEnumerable<BankCard> cards, string cardLastFour, int? cardTypeId)
    {
        var matches = cards
            .Where(card => string.Equals(NormalizeLastFour(card.LastFour), NormalizeLastFour(cardLastFour), StringComparison.Ordinal))
            .ToList();

        if (cardTypeId.HasValue)
        {
            matches = matches
                .Where(card => card.CardTypeId == cardTypeId.Value)
                .ToList();
        }

        return matches;
    }

    private static string DescribeCardType(int cardTypeId) =>
        cardTypeId switch
        {
            (int)CardType.Visa => "Visa",
            (int)CardType.MasterCard => "MasterCard",
            (int)CardType.Discover => "Discover",
            (int)CardType.AmericanExpress => "American Express",
            _ => "card"
        };

    private static string NormalizeLastFour(string? lastFour)
    {
        if (string.IsNullOrWhiteSpace(lastFour))
            return string.Empty;

        var digits = new string(lastFour.Where(char.IsDigit).ToArray());
        return digits.Length >= 4 ? digits[^4..] : digits;
    }
}

public class ReceiptExtractEnrichmentResult
{
    public int? BankCardId { get; set; }
    public int? PropertyOfficeId { get; set; }
    public IReadOnlyList<string> PropertyIds { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> DetectedPropertyCodes { get; set; } = Array.Empty<string>();
    public bool CardPaymentDetected { get; set; }
    public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
}
