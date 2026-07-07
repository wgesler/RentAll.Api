using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Reports;

public class RecapReportRowResponseDto
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
    public string Sdw { get; set; } = string.Empty;
    public string Fee { get; set; } = string.Empty;
    public string Payment { get; set; } = string.Empty;
    public string PrePayment { get; set; } = string.Empty;
    public string OwnerRent { get; set; } = string.Empty;
    public string OwnerExpense { get; set; } = string.Empty;
    public string OwnerPayment { get; set; } = string.Empty;
    public decimal ExpectedIncomeValue { get; set; }
    public decimal RentPlus4000Value { get; set; }
    public decimal SdwValue { get; set; }
    public decimal FeeValue { get; set; }
    public decimal PaymentValue { get; set; }
    public decimal PrePaymentValue { get; set; }
    public decimal OwnerRentValue { get; set; }
    public decimal OwnerExpenseValue { get; set; }
    public decimal OwnerPaymentValue { get; set; }
    public long SortDateValue { get; set; }
    public Guid? JournalEntryId { get; set; }
    public Guid? JournalEntryLineId { get; set; }

    public RecapReportRowResponseDto(RecapReportRow row)
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
        Sdw = row.Sdw;
        Fee = row.Fee;
        Payment = row.Payment;
        PrePayment = row.PrePayment;
        OwnerRent = row.OwnerRent;
        OwnerExpense = row.OwnerExpense;
        OwnerPayment = row.OwnerPayment;
        ExpectedIncomeValue = row.ExpectedIncomeValue;
        RentPlus4000Value = row.RentPlus4000Value;
        SdwValue = row.SdwValue;
        FeeValue = row.FeeValue;
        PaymentValue = row.PaymentValue;
        PrePaymentValue = row.PrePaymentValue;
        OwnerRentValue = row.OwnerRentValue;
        OwnerExpenseValue = row.OwnerExpenseValue;
        OwnerPaymentValue = row.OwnerPaymentValue;
        SortDateValue = row.SortDateValue;
        JournalEntryId = row.JournalEntryId;
        JournalEntryLineId = row.JournalEntryLineId;
    }
}
