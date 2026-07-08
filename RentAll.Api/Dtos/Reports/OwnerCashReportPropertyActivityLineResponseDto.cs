using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Reports;

public class OwnerCashReportPropertyActivityLineResponseDto
{
    public Guid PropertyId { get; set; }
    public int OfficeId { get; set; }
    public Guid? ActivityId { get; set; }
    public Guid? SourceId { get; set; }
    public Guid? JournalEntryLineId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public DateOnly ActivityDate { get; set; }
    public string AccountingPeriod { get; set; } = string.Empty;
    public string DocumentCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal ExpectedIncome { get; set; }
    public decimal ReceivedIncome { get; set; }
    public decimal Expenses { get; set; }
    public decimal OwnerPayment { get; set; }
    public decimal PrepaidIncome { get; set; }

    public OwnerCashReportPropertyActivityLineResponseDto(OwnerStatementPropertyActivityLine line)
    {
        PropertyId = line.PropertyId;
        OfficeId = line.OfficeId;
        ActivityId = line.ActivityId;
        SourceId = line.SourceId;
        JournalEntryLineId = line.JournalEntryLineId;
        ActivityType = line.ActivityType;
        ActivityDate = line.ActivityDate;
        AccountingPeriod = line.AccountingPeriod;
        DocumentCode = line.DocumentCode;
        Description = line.Description;
        ExpectedIncome = line.ExpectedIncome;
        ReceivedIncome = line.ReceivedIncome;
        PrepaidIncome = line.PrepaidIncome;
        Expenses = line.Expenses;
        OwnerPayment = line.OwnerPayment;
    }
}
