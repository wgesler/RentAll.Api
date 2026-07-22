namespace RentAll.Infrastructure.Entities.Accounting;

public class TransferEntity
{
    public Guid TransferId { get; set; }
    public string TransferCode { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public DateOnly TransferDate { get; set; }
    public DateOnly AccountingPeriod { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? PropertyId { get; set; }
    public int? BankAccountId { get; set; }
    public string BankAccountDisplayName { get; set; } = string.Empty;
    public string Splits { get; set; } = "[]";
    public int? PostingStatusId { get; set; }
    public bool HasBeenTransfered { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
    public string ModifiedByName { get; set; } = string.Empty;
}
