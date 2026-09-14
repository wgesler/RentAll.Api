using RentAll.Api.Dtos.Maintenances.Receipts;

namespace RentAll.Api.Dtos.Maintenances.ReceiptDrafts;

public static class ReceiptDraftPromotionMapper
{
    public static (bool IsValid, string? ErrorMessage, CreateReceiptDto? ReceiptDto) ToCreateReceiptDto(ReceiptDraft draft)
    {
        if (draft.IsPromoted)
            return (false, "Receipt draft has already been promoted", null);

        if (!draft.OfficeId.HasValue || draft.OfficeId.Value <= 0)
            return (false, "OfficeId is required to promote a receipt draft", null);

        if (!draft.ReceiptDate.HasValue || draft.ReceiptDate.Value == default)
            return (false, "ReceiptDate is required to promote a receipt draft", null);

        if (!draft.DueDate.HasValue || draft.DueDate.Value == default)
            return (false, "DueDate is required to promote a receipt draft", null);

        if (!draft.AccountingPeriod.HasValue || draft.AccountingPeriod.Value == default)
            return (false, "AccountingPeriod is required to promote a receipt draft", null);

        if (string.IsNullOrWhiteSpace(draft.Description))
            return (false, "Description is required to promote a receipt draft", null);

        if (draft.Splits == null || draft.Splits.Count == 0)
            return (false, "At least one split is required to promote a receipt draft", null);

        var hasCard = draft.BankCardId is > 0;
        var hasVendorId = draft.VendorId is { } vendorId && vendorId != Guid.Empty;
        var hasTypedVendor = !string.IsNullOrWhiteSpace(draft.VendorName) && !hasVendorId;
        if (!hasCard && hasTypedVendor)
            return (false, "Bank Card is required to keep a typed Vendor name on the receipt.", null);

        var receiptDto = new CreateReceiptDto
        {
            OrganizationId = draft.OrganizationId,
            OfficeId = draft.OfficeId.Value,
            PropertyIds = draft.PropertyIds ?? new List<Guid>(),
            ReceiptDate = draft.ReceiptDate.Value,
            DueDate = draft.DueDate.Value,
            AccountingPeriod = draft.AccountingPeriod.Value,
            BillNumber = draft.BillNumber ?? string.Empty,
            Amount = draft.Amount,
            Description = draft.Description.Trim(),
            BankCardId = hasCard ? draft.BankCardId : null,
            VendorId = hasCard ? null : draft.VendorId,
            VendorName = hasCard ? draft.VendorName : null,
            PaidDate = draft.PaidDate,
            PaymentDescription = draft.PaymentDescription,
            Splits = draft.Splits.Select(split => new ReceiptSplitDto(split)).ToList(),
            AgreementLineId = draft.AgreementLineId,
            ReceiptPath = draft.ReceiptPath,
            IsUtility = draft.IsUtility,
            BusinessPrivate = draft.BusinessPrivate,
            IsActive = true
        };

        var (isValid, errorMessage) = receiptDto.IsValid();
        if (!isValid)
            return (false, errorMessage ?? "Receipt draft is not ready for promotion", null);

        return (true, null, receiptDto);
    }
}
