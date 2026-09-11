using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Maintenances;

namespace RentAll.Api.Services;

public class ReceiptExtractEnrichmentService
{
    private readonly IAccountingRepository _accountingRepository;
    private readonly IPropertyRepository _propertyRepository;

    public ReceiptExtractEnrichmentService(
        IAccountingRepository accountingRepository,
        IPropertyRepository propertyRepository)
    {
        _accountingRepository = accountingRepository;
        _propertyRepository = propertyRepository;
    }

    public async Task<ReceiptExtractEnrichmentResult> EnrichAsync(
        ReceiptDocumentExtraction extraction,
        Guid organizationId,
        int? officeId,
        CancellationToken cancellationToken = default)
    {
        var warnings = extraction.Warnings.ToList();
        int? bankCardId = null;
        var propertyIds = new List<string>();

        if (!string.IsNullOrWhiteSpace(extraction.CardLastFour))
        {
            if (!officeId.HasValue || officeId.Value <= 0)
            {
                warnings.Add("Credit card ending detected but office is required to match a bank card.");
            }
            else
            {
                var cards = await _accountingRepository.GetBankCardsByOfficeIdAsync(organizationId, officeId.Value);
                var matches = cards
                    .Where(card => string.Equals(NormalizeLastFour(card.LastFour), extraction.CardLastFour, StringComparison.Ordinal))
                    .ToList();

                if (extraction.CardTypeId.HasValue)
                {
                    matches = matches
                        .Where(card => card.CardTypeId == extraction.CardTypeId.Value)
                        .ToList();
                }

                if (matches.Count == 1)
                {
                    bankCardId = matches[0].BankCardId;
                }
                else if (matches.Count > 1)
                {
                    warnings.Add($"Multiple bank cards match ending {extraction.CardLastFour}. Select the card manually.");
                }
                else
                {
                    var cardTypeHint = extraction.CardTypeId.HasValue ? " and card type" : string.Empty;
                    warnings.Add($"No bank card found for ending {extraction.CardLastFour}{cardTypeHint} in this office.");
                }
            }
        }

        foreach (var code in extraction.DetectedPropertyCodes.OrderByDescending(code => code.Length))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var property = await _propertyRepository.GetPropertyByCodeAsync(code, organizationId);
            if (property == null || property.PropertyId == Guid.Empty)
                continue;

            var propertyId = property.PropertyId.ToString();
            if (!propertyIds.Contains(propertyId, StringComparer.OrdinalIgnoreCase))
                propertyIds.Add(propertyId);
        }

        if (extraction.DetectedPropertyCodes.Count > 0 && propertyIds.Count == 0)
        {
            warnings.Add($"Property code(s) {string.Join(", ", extraction.DetectedPropertyCodes)} were not found.");
        }
        else if (propertyIds.Count > 1)
        {
            warnings.Add("Multiple property codes were detected on the receipt. Review the selected properties.");
        }

        return new ReceiptExtractEnrichmentResult
        {
            BankCardId = bankCardId,
            PropertyIds = propertyIds,
            Warnings = warnings
        };
    }

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
    public IReadOnlyList<string> PropertyIds { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
}
