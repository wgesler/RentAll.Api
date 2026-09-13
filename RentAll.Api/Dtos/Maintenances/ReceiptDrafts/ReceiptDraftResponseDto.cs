using RentAll.Api.Dtos.Maintenances.Receipts;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Maintenances.ReceiptDrafts;

public class ReceiptDraftResponseDto
{
    public Guid ReceiptDraftId { get; set; }
    public string DraftCode { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public int? OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public List<Guid> PropertyIds { get; set; } = new();
    public DateOnly? ReceiptDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateOnly? AccountingPeriod { get; set; }
    public string? BillNumber { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public int? BankCardId { get; set; }
    public string BankCardDisplayName { get; set; } = string.Empty;
    public Guid? VendorId { get; set; }
    public string? VendorName { get; set; }
    public decimal PaidAmount { get; set; }
    public DateOnly? PaidDate { get; set; }
    public string? PaymentDescription { get; set; }
    public List<ReceiptSplitDto> Splits { get; set; } = new();
    public int? AgreementLineId { get; set; }
    public string? AgreementLineNotes { get; set; }
    public string? ReceiptPath { get; set; }
    public FileDetails? FileDetails { get; set; }
    public int PaymentTypeId { get; set; }
    public bool CheckPrinted { get; set; }
    public bool IsUtility { get; set; }
    public bool BusinessPrivate { get; set; }
    public ReceiptDraftSourceFlags DraftSourceFlags { get; set; }
    public bool HasUploadSource { get; set; }
    public bool HasStatementImportSource { get; set; }
    public Guid? PromotedReceiptId { get; set; }
    public string? PromotedReceiptCode { get; set; }
    public DateTimeOffset? PromotedOn { get; set; }
    public string PromotedBy { get; set; } = string.Empty;
    public bool IsPromoted { get; set; }
    public string? ExtractionJson { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset ModifiedOn { get; set; }
    public string ModifiedBy { get; set; } = string.Empty;

    public ReceiptDraftResponseDto(ReceiptDraft draft)
    {
        ReceiptDraftId = draft.ReceiptDraftId;
        DraftCode = draft.DraftCode;
        OrganizationId = draft.OrganizationId;
        OfficeId = draft.OfficeId;
        OfficeName = draft.OfficeName;
        PropertyIds = draft.PropertyIds;
        ReceiptDate = draft.ReceiptDate;
        DueDate = draft.DueDate;
        AccountingPeriod = draft.AccountingPeriod;
        BillNumber = draft.BillNumber;
        Amount = draft.Amount;
        Description = draft.Description;
        BankCardId = draft.BankCardId;
        BankCardDisplayName = draft.BankCardDisplayName;
        VendorId = draft.VendorId;
        VendorName = draft.VendorName;
        PaidAmount = draft.PaidAmount;
        PaidDate = draft.PaidDate;
        PaymentDescription = draft.PaymentDescription;
        Splits = (draft.Splits ?? new List<ReceiptSplit>()).Select(split => new ReceiptSplitDto(split)).ToList();
        AgreementLineId = draft.AgreementLineId;
        AgreementLineNotes = draft.AgreementLineNotes;
        ReceiptPath = draft.ReceiptPath;
        FileDetails = draft.FileDetails;
        PaymentTypeId = draft.PaymentTypeId;
        CheckPrinted = draft.CheckPrinted;
        IsUtility = draft.IsUtility;
        BusinessPrivate = draft.BusinessPrivate;
        DraftSourceFlags = draft.DraftSourceFlags;
        HasUploadSource = draft.DraftSourceFlags.HasFlag(ReceiptDraftSourceFlags.Upload);
        HasStatementImportSource = draft.DraftSourceFlags.HasFlag(ReceiptDraftSourceFlags.StatementImport);
        PromotedReceiptId = draft.PromotedReceiptId;
        PromotedReceiptCode = draft.PromotedReceiptCode;
        PromotedOn = draft.PromotedOn;
        PromotedBy = draft.PromotedByName;
        IsPromoted = draft.IsPromoted;
        ExtractionJson = draft.ExtractionJson;
        IsActive = draft.IsActive;
        CreatedOn = draft.CreatedOn;
        CreatedBy = draft.CreatedByName;
        ModifiedOn = draft.ModifiedOn;
        ModifiedBy = draft.ModifiedByName;
    }
}
