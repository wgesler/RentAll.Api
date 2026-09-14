using RentAll.Domain.Models.Maintenances;
using RentAll.Infrastructure.Services;

namespace RentAll.Api.Dtos.Maintenances.Receipts;

public class CreditReportSplitDto
{
    public Guid? WorkOrderId { get; set; }
    public string? WorkOrderCode { get; set; }
    public int ReceiptTypeId { get; set; }
}

public class CreditReportLineDto
{
    public DateOnly? ChargeDate { get; set; }
    public decimal Amount { get; set; }
    public string? VendorName { get; set; }
    public Guid? VendorId { get; set; }
    public string? CardLastFour { get; set; }
    public int? BankCardId { get; set; }
    public string? BankCardDisplayName { get; set; }
    public int? CardTypeId { get; set; }
    public string? Description { get; set; }
    public Guid? ReceiptId { get; set; }
    public string? ReceiptCode { get; set; }
    public Guid? ReceiptDraftId { get; set; }
    public string? DraftCode { get; set; }
    public bool IsUtility { get; set; }
    public List<CreditReportSplitDto> Splits { get; set; } = new();
}

public class CreditReportResponseDto
{
    public string? FileName { get; set; }
    public List<CreditReportLineDto> CompleteMatches { get; set; } = new();
    public List<CreditReportLineDto> DraftMatches { get; set; } = new();
    public List<CreditReportLineDto> CreatedDrafts { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public static CreditReportLineDto FromLine(CreditCardStatementLine line, Receipt? receipt = null, ReceiptDraft? draft = null, Guid? vendorId = null, int? bankCardId = null, int? cardTypeId = null, BankCard? resolvedCard = null)
    {
        var isMatched = receipt != null || draft != null;
        return new CreditReportLineDto
        {
            ChargeDate = isMatched ? (receipt?.ReceiptDate ?? draft?.ReceiptDate ?? line.ChargeDate) : line.ChargeDate,
            Amount = isMatched ? (receipt?.Amount ?? draft?.Amount ?? line.Amount ?? 0) : (line.Amount ?? 0),
            VendorName = ResolveDisplayVendorName(isMatched ? (receipt?.VendorName ?? draft?.VendorName) : line.VendorName, line.VendorName),
            VendorId = isMatched ? (receipt?.VendorId ?? draft?.VendorId) : vendorId,
            CardLastFour = isMatched ? (resolvedCard?.LastFour ?? line.CardLastFour) : (line.CardLastFour ?? resolvedCard?.LastFour),
            BankCardId = isMatched ? (receipt?.BankCardId ?? draft?.BankCardId) : (bankCardId ?? resolvedCard?.BankCardId),
            BankCardDisplayName = isMatched ? ResolveMatchedCardDisplayName(receipt, draft) : (resolvedCard?.DisplayName ?? null),
            CardTypeId = isMatched ? (resolvedCard?.CardTypeId ?? line.CardTypeId ?? cardTypeId) : (cardTypeId ?? line.CardTypeId ?? resolvedCard?.CardTypeId),
            Description = isMatched ? (receipt?.Description ?? draft?.Description) : null,
            ReceiptId = receipt?.ReceiptId,
            ReceiptCode = receipt?.ReceiptCode,
            ReceiptDraftId = draft?.ReceiptDraftId,
            DraftCode = draft?.DraftCode,
            IsUtility = receipt?.IsUtility ?? draft?.IsUtility ?? false,
            Splits = (receipt?.Splits ?? draft?.Splits ?? []).Select(split => new CreditReportSplitDto { WorkOrderId = split.WorkOrderId, WorkOrderCode = split.WorkOrderCode ?? split.WorkOrder, ReceiptTypeId = split.ReceiptTypeId }).ToList()
        };
    }

    private static string? ResolveDisplayVendorName(string? preferred, string? statement)
    {
        return CreditCardStatementLineParser.CleanVendorName(preferred)
            ?? CreditCardStatementLineParser.CleanVendorName(statement)
            ?? preferred
            ?? statement;
    }

    private static string? ResolveMatchedCardDisplayName(Receipt? receipt, ReceiptDraft? draft)
    {
        if (receipt?.BankCardId > 0 && IsOfficeCardName(receipt.BankCardDisplayName))
            return receipt.BankCardDisplayName.Trim();

        if (draft?.BankCardId > 0 && IsOfficeCardName(draft.BankCardDisplayName))
            return draft.BankCardDisplayName.Trim();

        return null;
    }

    private static bool IsOfficeCardName(string? value)
    {
        var name = (value ?? string.Empty).Trim();
        return name.Length > 0 && !name.Equals("Bill", StringComparison.OrdinalIgnoreCase);
    }
}
