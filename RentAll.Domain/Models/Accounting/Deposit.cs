namespace RentAll.Domain.Models;

public class Deposit
{
    public Guid DepositId { get; set; }
    public string DepositCode { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public List<Guid> PropertyIds { get; set; } = new();
    public DateOnly DepositDate { get; set; }
    public DateOnly AccountingPeriod { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? PropertyId { get; set; }
    public int? BankAccountId { get; set; }
    public string BankAccountDisplayName { get; set; } = string.Empty;
    public List<DepositSplit> Splits { get; set; } = new();
    public Guid? JournalEntryId { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
    public string ModifiedByName { get; set; } = string.Empty;
}
