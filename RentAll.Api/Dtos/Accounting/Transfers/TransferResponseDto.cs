namespace RentAll.Api.Dtos.Accounting.Transfers;

public class TransferResponseDto
{
    public Guid TransferId { get; set; }
    public string TransferCode { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public Guid? PropertyId { get; set; }
    public List<Guid> PropertyIds { get; set; } = new List<Guid>();
    public DateOnly TransferDate { get; set; }
    public DateOnly AccountingPeriod { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? BankAccountId { get; set; }
    public string BankAccountDisplayName { get; set; } = string.Empty;
    public List<TransferSplitDto> Splits { get; set; } = new List<TransferSplitDto>();
    public Guid? JournalEntryId { get; set; }
    public int? PostingStatusId { get; set; }
    public bool HasBeenTransfered { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset ModifiedOn { get; set; }
    public string ModifiedBy { get; set; }

    public TransferResponseDto(Transfer transfer)
    {
        TransferId = transfer.TransferId;
        TransferCode = transfer.TransferCode;
        OrganizationId = transfer.OrganizationId;
        OfficeId = transfer.OfficeId;
        OfficeName = transfer.OfficeName;
        PropertyId = transfer.PropertyId is { } headerPropertyId && headerPropertyId != Guid.Empty
            ? headerPropertyId
            : null;
        PropertyIds = transfer.PropertyIds ?? new List<Guid>();
        TransferDate = transfer.TransferDate;
        AccountingPeriod = transfer.AccountingPeriod;
        Amount = transfer.Amount;
        Description = transfer.Description;
        BankAccountId = transfer.BankAccountId;
        BankAccountDisplayName = transfer.BankAccountDisplayName;
        Splits = (transfer.Splits ?? new List<TransferSplit>()).Select(split => new TransferSplitDto(split)).ToList();
        JournalEntryId = transfer.JournalEntryId;
        PostingStatusId = transfer.PostingStatusId;
        HasBeenTransfered = transfer.HasBeenTransfered;
        IsActive = transfer.IsActive;
        CreatedOn = transfer.CreatedOn;
        CreatedBy = transfer.CreatedByName;
        ModifiedOn = transfer.ModifiedOn;
        ModifiedBy = transfer.ModifiedByName;
    }
}
