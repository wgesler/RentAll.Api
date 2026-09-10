namespace RentAll.Infrastructure.Entities.Accounting;

public class OwnerReportMonthlyAnchorEntity
{
    public Guid OwnerReportMonthlyAnchorId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid PropertyId { get; set; }
    public DateTime PeriodMonth { get; set; }
    public DateTime PeriodStartDate { get; set; }
    public DateTime PeriodEndDate { get; set; }
    public decimal StartingBalance { get; set; }
    public decimal ReceivedIncome { get; set; }
    public decimal OwnerExpenses { get; set; }
    public decimal OwnerPayment { get; set; }
    public decimal OwnerPaymentPaid { get; set; }
    public decimal EndingBalance { get; set; }
    public decimal WorkingCapital { get; set; }
    public byte AnchorStatusId { get; set; }
    public int CalculationVersion { get; set; }
    public bool IsDirty { get; set; }
    public DateTimeOffset CalculatedOn { get; set; }
}

public class OwnerReportRecapLoadPlanEntity
{
    public bool UseNarrowRecapLoad { get; set; }
    public DateTime? RecapLoadStartDate { get; set; }
    public int PropertyCount { get; set; }
    public int AnchoredPropertyCount { get; set; }
    public DateTime PriorPeriodMonth { get; set; }
}
