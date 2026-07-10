using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Maintenances.Receipts;

public class CreateReceiptDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public List<Guid> PropertyIds { get; set; } = new List<Guid>();
    public DateOnly ReceiptDate { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly AccountingPeriod { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly? PaidDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? BankCardId { get; set; }
    public Guid? VendorId { get; set; }
    public string? VendorName { get; set; }
    public List<ReceiptSplitDto> Splits { get; set; } = new List<ReceiptSplitDto>();
    public int? AgreementLineId { get; set; }
    public string? ReceiptPath { get; set; }
    public FileDetails? FileDetails { get; set; }
    public bool IsUtility { get; set; }
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        var requiresPropertyIds = (Splits ?? new List<ReceiptSplitDto>())
            .Any(split => split.ReceiptTypeId != (int)ReceiptType.Company);

        if (requiresPropertyIds && (PropertyIds == null || PropertyIds.Count == 0))
            return (false, "At least one PropertyId is required");

        if (PropertyIds.Any(id => id == Guid.Empty))
            return (false, "PropertyIds cannot contain empty Guid values");

        if (ReceiptDate == default)
            return (false, "ReceiptDate is required");

        if (string.IsNullOrWhiteSpace(Description))
            return (false, "Description is required");

        if (Splits == null || Splits.Count == 0)
            return (false, "At least one split is required");

        foreach (var split in Splits)
        {
            var (isValid, errorMessage) = split.IsValid();
            if (!isValid)
                return (false, $"Split validation failed: {errorMessage}");
        }

        return (true, null);
    }

    public Receipt ToModel(string receiptCode, Guid currentUser)
    {
        var normalizedBankCardId = BankCardId is > 0 ? BankCardId : null;
        var initialPaidAmount = normalizedBankCardId.HasValue ? Amount : 0;
        return new Receipt
        {
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            ReceiptCode = receiptCode.Trim(),
            PropertyIds = PropertyIds,
            ReceiptDate = ReceiptDate,
            DueDate = DueDate,
            AccountingPeriod = AccountingPeriod,
            BillNumber = BillNumber,
            Amount = Amount,
            PaidAmount = initialPaidAmount,
            PaidDate = PaidDate,
            Description = Description,
            BankCardId = normalizedBankCardId,
            VendorId = VendorId,
            VendorName = VendorName,
            Splits = Splits.Select(split => split.ToModel()).ToList(),
            AgreementLineId = AgreementLineId,
            ReceiptPath = null,
            PaymentTypeId = 0,
            CheckPrinted = false,
            IsUtility = IsUtility,
            IsActive = true,
            CreatedBy = currentUser
        };
    }
}
