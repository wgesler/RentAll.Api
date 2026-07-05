using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Accounting.OwnerStatements;

public class OwnerStatementPropertyActivityLineResponseDto
{
    public Guid PropertyId { get; set; }
    public int OfficeId { get; set; }
    public Guid? ActivityId { get; set; }
    public Guid? SourceId { get; set; }
    public Guid? JournalEntryLineId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public DateOnly ActivityDate { get; set; }
    public string DocumentCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal ExpectedIncome { get; set; }
    public decimal ReceivedIncome { get; set; }
    public decimal Expenses { get; set; }

    public OwnerStatementPropertyActivityLineResponseDto(OwnerStatementPropertyActivityLine line)
    {
        PropertyId = line.PropertyId;
        OfficeId = line.OfficeId;
        ActivityId = line.ActivityId;
        SourceId = line.SourceId;
        JournalEntryLineId = line.JournalEntryLineId;
        ActivityType = line.ActivityType;
        ActivityDate = line.ActivityDate;
        DocumentCode = line.DocumentCode;
        Description = line.Description;
        ExpectedIncome = line.ExpectedIncome;
        ReceivedIncome = line.ReceivedIncome;
        Expenses = line.Expenses;
    }
}
