namespace RentAll.Domain.Models;

public class OwnerReportMonthlyAnchor
{
    public Guid OwnerReportMonthlyAnchorId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid PropertyId { get; set; }
    public DateOnly PeriodMonth { get; set; }
    public DateOnly PeriodStartDate { get; set; }
    public DateOnly PeriodEndDate { get; set; }
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

public class OwnerReportRecapLoadPlan
{
    public bool UseNarrowRecapLoad { get; set; }
    public DateOnly? RecapLoadStartDate { get; set; }
    public int PropertyCount { get; set; }
    public int AnchoredPropertyCount { get; set; }
    public DateOnly PriorPeriodMonth { get; set; }
}

public static class OwnerReportAnchorStatus
{
    public const byte Provisional = 0;
    public const byte Closed = 1;
    public const byte Dirty = 2;
}

public static class OwnerReportAnchorCalculation
{
    public const int CurrentVersion = 1;
}
