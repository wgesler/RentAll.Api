namespace RentAll.Api.Dtos.Accounting.BankDeposits;

public class DepositResponseDto
{
    public Guid DepositId { get; set; }
    public string DepositCode { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public Guid? PropertyId { get; set; }
    public List<Guid> PropertyIds { get; set; } = new List<Guid>();
    public DateOnly DepositDate { get; set; }
    public DateOnly AccountingPeriod { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? BankAccountId { get; set; }
    public string BankAccountDisplayName { get; set; } = string.Empty;
    public List<DepositSplitDto> Splits { get; set; } = new List<DepositSplitDto>();
    public Guid? JournalEntryId { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset ModifiedOn { get; set; }
    public string ModifiedBy { get; set; }

    public DepositResponseDto(Deposit deposit)
    {
        DepositId = deposit.DepositId;
        DepositCode = deposit.DepositCode;
        OrganizationId = deposit.OrganizationId;
        OfficeId = deposit.OfficeId;
        OfficeName = deposit.OfficeName;
        PropertyId = deposit.PropertyId is { } headerPropertyId && headerPropertyId != Guid.Empty
            ? headerPropertyId
            : null;
        PropertyIds = deposit.PropertyIds ?? new List<Guid>();
        DepositDate = deposit.DepositDate;
        AccountingPeriod = deposit.AccountingPeriod;
        Amount = deposit.Amount;
        Description = deposit.Description;
        BankAccountId = deposit.BankAccountId;
        BankAccountDisplayName = deposit.BankAccountDisplayName;
        Splits = (deposit.Splits ?? new List<DepositSplit>()).Select(split => new DepositSplitDto(split)).ToList();
        JournalEntryId = deposit.JournalEntryId;
        IsActive = deposit.IsActive;
        CreatedOn = deposit.CreatedOn;
        CreatedBy = deposit.CreatedByName;
        ModifiedOn = deposit.ModifiedOn;
        ModifiedBy = deposit.ModifiedByName;
    }
}
