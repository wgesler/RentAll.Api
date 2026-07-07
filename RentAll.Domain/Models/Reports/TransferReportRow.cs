namespace RentAll.Domain.Models;

public class TransferReportRow
{
    public string PropertyCode { get; set; } = string.Empty;
    public string ReservationCode { get; set; } = string.Empty;
    public string AccountingPeriod { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string JournalEntryCode { get; set; } = string.Empty;
    public int? SourceTypeId { get; set; }
    public Guid? SourceId { get; set; }
    public bool SourceLinkable { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public int OfficeId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? ReservationId { get; set; }
    public string TransactionDate { get; set; } = string.Empty;
    public string ExpectedIncome { get; set; } = string.Empty;
    public string RentPlus4000 { get; set; } = string.Empty;
    public string OwnerRent { get; set; } = string.Empty;
    public string Business { get; set; } = string.Empty;
    public string SecurityDeposit { get; set; } = string.Empty;
    public string Sdw { get; set; } = string.Empty;
    public string Fee { get; set; } = string.Empty;
    public string RunningTotalUnposted { get; set; } = string.Empty;
    public decimal ExpectedIncomeValue { get; set; }
    public decimal RentPlus4000Value { get; set; }
    public decimal OwnerRentValue { get; set; }
    public decimal BusinessValue { get; set; }
    public decimal SecurityDepositValue { get; set; }
    public decimal SdwValue { get; set; }
    public decimal FeeValue { get; set; }
    public decimal RunningTotalUnpostedValue { get; set; }
    public long SortDateValue { get; set; }
    public Guid? JournalEntryId { get; set; }
    public Guid? JournalEntryLineId { get; set; }
}
