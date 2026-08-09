using RentAll.Domain.Constants;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Maintenances.Receipts;

public class UpdateReceiptDto
{
    public Guid ReceiptId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public List<Guid> PropertyIds { get; set; } = new List<Guid>();
    public DateOnly ReceiptDate { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly AccountingPeriod { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? BankCardId { get; set; }
    public Guid? VendorId { get; set; }
    public string? VendorName { get; set; }
    public decimal PaidAmount { get; set; }
    public DateOnly? PaidDate { get; set; }
    public string? PaymentDescription { get; set; }
    public List<ReceiptSplitDto> Splits { get; set; } = new List<ReceiptSplitDto>();
    public int? AgreementLineId { get; set; }
    public string? ReceiptPath { get; set; }
    public FileDetails? FileDetails { get; set; }
    public int PaymentTypeId { get; set; }
    public bool CheckPrinted { get; set; }
    public bool IsUtility { get; set; }
    public bool BusinessPrivate { get; set; }
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (ReceiptId == Guid.Empty)
            return (false, "ReceiptId is required");

        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        var requiresPropertyIds = (Splits ?? new List<ReceiptSplitDto>())
            .Any(split => split.ReceiptTypeId != (int)ReceiptType.Company);

        var propertyIds = PropertyIds ?? new List<Guid>();
        var hasCompanyPropertyId = propertyIds.Any(ReceiptPropertyConstants.IsCompanyPropertyId);
        var hasRealPropertyIds = propertyIds.Any(id => !ReceiptPropertyConstants.IsCompanyPropertyId(id));

        if (requiresPropertyIds && !hasRealPropertyIds && !hasCompanyPropertyId)
            return (false, "At least one PropertyId is required");

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

    public Receipt ToModel(Guid currentUser)
    {
        return new Receipt
        {
            ReceiptId = ReceiptId,
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            PropertyIds = PropertyIds,
            ReceiptDate = ReceiptDate,
            DueDate = DueDate,
            AccountingPeriod = AccountingPeriod,
            BillNumber = BillNumber,
            Amount = Amount,
            Description = Description,
            BankCardId = BankCardId is > 0 ? BankCardId : null,
            VendorId = VendorId,
            VendorName = VendorName,
            PaidAmount = PaidAmount,
            PaidDate = PaidDate,
            PaymentDescription = PaymentDescription,
            Splits = Splits.Select(split => split.ToModel()).ToList(),
            AgreementLineId = AgreementLineId,
            ReceiptPath = ReceiptPath,
            PaymentTypeId = PaymentTypeId,
            CheckPrinted = CheckPrinted,
            IsUtility = IsUtility,
            BusinessPrivate = BusinessPrivate,
            IsActive = IsActive,
            ModifiedBy = currentUser
        };
    }
}
