namespace RentAll.Domain.Models;

public class Invoice
{
    public Guid InvoiceId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public string InvoiceCode { get; set; } = string.Empty;
    public Guid? ReservationId { get; set; }
    public string? ReservationCode { get; set; }
    public Guid? PropertyId { get; set; }
    public string? PropertyCode { get; set; }
    public Guid? ContactId { get; set; }
    public string? ContactName { get; set; }
    public string? TenantName { get; set; }
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? ResponsibleParty { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly AccountingPeriod { get; set; }
    public string? InvoicePeriod { get; set; }
    public int? PostingStatusId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public List<LedgerLine> LedgerLines { get; set; } = new List<LedgerLine>();
    public int? BilledId { get; set; }
    public bool? BilledIgnore { get; set; }
    public int? BilledDaysStayed { get; set; }
    public int? BilledDaysBilled { get; set; }
    public DateOnly? BilledMonthStart { get; set; }
    public DateOnly? BilledMonthEnd { get; set; }
    public DateOnly? BilledPeriodStart { get; set; }
    public DateOnly? BilledPeriodEnd { get; set; }
    public DateOnly? BilledStartDate { get; set; }
    public DateOnly? BilledEndDate { get; set; }
    public string? BilledRentalFeeLines { get; set; }
    public bool ReferralBillCreated { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }

}
