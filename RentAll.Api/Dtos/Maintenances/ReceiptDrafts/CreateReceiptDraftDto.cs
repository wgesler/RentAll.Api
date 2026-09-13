using RentAll.Api.Dtos.Maintenances.Receipts;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Maintenances.ReceiptDrafts;

public class CreateReceiptDraftDto
{
    public Guid OrganizationId { get; set; }
    public int? OfficeId { get; set; }
    public List<Guid> PropertyIds { get; set; } = new();
    public DateOnly? ReceiptDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateOnly? AccountingPeriod { get; set; }
    public string? BillNumber { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public int? BankCardId { get; set; }
    public Guid? VendorId { get; set; }
    public string? VendorName { get; set; }
    public DateOnly? PaidDate { get; set; }
    public string? PaymentDescription { get; set; }
    public List<ReceiptSplitDto> Splits { get; set; } = new();
    public int? AgreementLineId { get; set; }
    public string? ReceiptPath { get; set; }
    public FileDetails? FileDetails { get; set; }
    public bool IsUtility { get; set; }
    public bool BusinessPrivate { get; set; }
    public bool IsActive { get; set; } = true;
    public ReceiptDraftSourceFlags DraftSourceFlags { get; set; }
    public string? ExtractionJson { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId.HasValue && OfficeId.Value < 0)
            return (false, "OfficeId must be null or a positive integer");

        foreach (var split in Splits ?? new List<ReceiptSplitDto>())
        {
            if (!Enum.IsDefined(typeof(ReceiptType), split.ReceiptTypeId))
                return (false, $"Invalid ReceiptTypeId value: {split.ReceiptTypeId}");
        }

        return (true, null);
    }

    public ReceiptDraft ToModel(string draftCode, Guid currentUser)
    {
        return new ReceiptDraft
        {
            OrganizationId = OrganizationId,
            OfficeId = OfficeId is > 0 ? OfficeId : null,
            DraftCode = draftCode.Trim(),
            PropertyIds = PropertyIds ?? new List<Guid>(),
            ReceiptDate = ReceiptDate,
            DueDate = DueDate,
            AccountingPeriod = AccountingPeriod,
            BillNumber = BillNumber,
            Amount = Amount,
            Description = Description,
            BankCardId = BankCardId is > 0 ? BankCardId : null,
            VendorId = VendorId,
            VendorName = VendorName,
            PaidAmount = BankCardId is > 0 ? Amount : 0,
            PaidDate = PaidDate,
            PaymentDescription = PaymentDescription,
            Splits = (Splits ?? new List<ReceiptSplitDto>()).Select(split => split.ToModel()).ToList(),
            AgreementLineId = AgreementLineId,
            ReceiptPath = null,
            PaymentTypeId = 0,
            CheckPrinted = false,
            IsUtility = IsUtility,
            BusinessPrivate = BusinessPrivate,
            DraftSourceFlags = DraftSourceFlags,
            ExtractionJson = ExtractionJson,
            IsActive = IsActive,
            CreatedBy = currentUser,
            ModifiedBy = currentUser
        };
    }
}
