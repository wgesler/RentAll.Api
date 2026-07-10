namespace RentAll.Api.Dtos.Reports;

public class TransferReportRowResponseDto
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
    public decimal ExpectedIncomeValue { get; set; }
    public decimal RentPlus4000Value { get; set; }
    public decimal OwnerRentValue { get; set; }
    public decimal BusinessValue { get; set; }
    public decimal SecurityDepositValue { get; set; }
    public decimal SdwValue { get; set; }
    public decimal FeeValue { get; set; }
    public long SortDateValue { get; set; }
    public Guid? JournalEntryId { get; set; }
    public Guid? JournalEntryLineId { get; set; }

    public TransferReportRowResponseDto(TransferReportRow row)
    {
        PropertyCode = row.PropertyCode;
        ReservationCode = row.ReservationCode;
        AccountingPeriod = row.AccountingPeriod;
        Source = row.Source;
        JournalEntryCode = row.JournalEntryCode;
        SourceTypeId = row.SourceTypeId;
        SourceId = row.SourceId;
        SourceLinkable = row.SourceLinkable;
        ActivityType = row.ActivityType;
        OfficeId = row.OfficeId;
        PropertyId = row.PropertyId;
        ReservationId = row.ReservationId;
        TransactionDate = row.TransactionDate;
        ExpectedIncome = row.ExpectedIncome;
        RentPlus4000 = row.RentPlus4000;
        OwnerRent = row.OwnerRent;
        Business = row.Business;
        SecurityDeposit = row.SecurityDeposit;
        Sdw = row.Sdw;
        Fee = row.Fee;
        ExpectedIncomeValue = row.ExpectedIncomeValue;
        RentPlus4000Value = row.RentPlus4000Value;
        OwnerRentValue = row.OwnerRentValue;
        BusinessValue = row.BusinessValue;
        SecurityDepositValue = row.SecurityDepositValue;
        SdwValue = row.SdwValue;
        FeeValue = row.FeeValue;
        SortDateValue = row.SortDateValue;
        JournalEntryId = row.JournalEntryId;
        JournalEntryLineId = row.JournalEntryLineId;
    }
}
