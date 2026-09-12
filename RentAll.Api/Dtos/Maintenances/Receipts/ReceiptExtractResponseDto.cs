using RentAll.Domain.Enums;
using RentAll.Domain.Models.Maintenances;

namespace RentAll.Api.Dtos.Maintenances.Receipts;

public class ReceiptExtractSplitResponseDto
{
    public decimal? Amount { get; set; }
    public string? Description { get; set; }
    public int? ReceiptTypeId { get; set; }
}

public class ReceiptExtractResponseDto
{
    public string Key { get; set; } = "document-intelligence";
    public string? ReceiptDate { get; set; }
    public string? DueDate { get; set; }
    public string? AccountingPeriod { get; set; }
    public decimal? Amount { get; set; }
    public string? Description { get; set; }
    public string? VendorName { get; set; }
    public string? BillNumber { get; set; }
    public int? BankCardId { get; set; }
    public int? OfficeId { get; set; }
    public bool CardPaymentDetected { get; set; }
    public IReadOnlyList<string> PropertyIds { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> DetectedPropertyCodes { get; set; } = Array.Empty<string>();
    public ReceiptExtractSplitResponseDto? Split { get; set; }
    public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, double?> FieldConfidences { get; set; } = new Dictionary<string, double?>();

    public static ReceiptExtractResponseDto FromExtraction(
        ReceiptDocumentExtraction extraction,
        int? bankCardId = null,
        IReadOnlyList<string>? propertyIds = null,
        IReadOnlyList<string>? warnings = null,
        IReadOnlyList<string>? detectedPropertyCodes = null,
        bool cardPaymentDetected = false,
        int? officeId = null)
    {
        var receiptDate = extraction.ReceiptDate?.ToString("yyyy-MM-dd");
        var amount = extraction.Amount;
        var description = extraction.Description?.Trim();

        return new ReceiptExtractResponseDto
        {
            ReceiptDate = receiptDate,
            DueDate = receiptDate,
            AccountingPeriod = receiptDate,
            Amount = amount,
            Description = description,
            VendorName = extraction.VendorName?.Trim(),
            BillNumber = extraction.BillNumber?.Trim(),
            BankCardId = bankCardId,
            OfficeId = officeId,
            CardPaymentDetected = cardPaymentDetected,
            PropertyIds = propertyIds ?? Array.Empty<string>(),
            DetectedPropertyCodes = detectedPropertyCodes ?? extraction.DetectedPropertyCodes,
            Split = amount.HasValue || !string.IsNullOrWhiteSpace(description)
                ? new ReceiptExtractSplitResponseDto
                {
                    Amount = amount,
                    Description = description,
                    ReceiptTypeId = (int)ReceiptType.Tenant
                }
                : null,
            Warnings = warnings ?? extraction.Warnings,
            FieldConfidences = extraction.FieldConfidences
        };
    }
}
