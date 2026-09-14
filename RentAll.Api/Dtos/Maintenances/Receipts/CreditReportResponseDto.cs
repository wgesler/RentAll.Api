using RentAll.Domain.Models;
using RentAll.Domain.Models.Maintenances;

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
        return new CreditReportLineDto
        {
            ChargeDate = line.ChargeDate ?? receipt?.ReceiptDate ?? draft?.ReceiptDate,
            Amount = line.Amount ?? receipt?.Amount ?? draft?.Amount ?? 0,
            VendorName = line.VendorName ?? receipt?.VendorName ?? draft?.VendorName,
            VendorId = vendorId ?? receipt?.VendorId ?? draft?.VendorId,
            CardLastFour = line.CardLastFour ?? resolvedCard?.LastFour,
            BankCardId = bankCardId ?? receipt?.BankCardId ?? draft?.BankCardId ?? resolvedCard?.BankCardId,
            BankCardDisplayName = ResolveCardDisplayName(receipt, draft, resolvedCard),
            CardTypeId = cardTypeId ?? line.CardTypeId ?? resolvedCard?.CardTypeId,
            Description = receipt?.Description ?? draft?.Description,
            ReceiptId = receipt?.ReceiptId,
            ReceiptCode = receipt?.ReceiptCode,
            ReceiptDraftId = draft?.ReceiptDraftId,
            DraftCode = draft?.DraftCode,
            IsUtility = receipt?.IsUtility ?? draft?.IsUtility ?? false,
            Splits = (receipt?.Splits ?? draft?.Splits ?? []).Select(split => new CreditReportSplitDto { WorkOrderId = split.WorkOrderId, WorkOrderCode = split.WorkOrderCode ?? split.WorkOrder, ReceiptTypeId = split.ReceiptTypeId }).ToList()
        };
    }

    private static string? ResolveCardDisplayName(Receipt? receipt, ReceiptDraft? draft, BankCard? resolvedCard)
    {
        if (!string.IsNullOrWhiteSpace(resolvedCard?.DisplayName))
            return resolvedCard.DisplayName.Trim();

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
