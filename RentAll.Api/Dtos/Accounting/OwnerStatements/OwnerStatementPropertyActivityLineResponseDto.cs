using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Accounting.OwnerStatements;

public class OwnerStatementPropertyActivityLineResponseDto
{
    public Guid? ActivityId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public DateOnly ActivityDate { get; set; }
    public string DocumentCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal ExpectedIncome { get; set; }
    public decimal Expenses { get; set; }

    public OwnerStatementPropertyActivityLineResponseDto(OwnerStatementPropertyActivityLine line)
    {
        ActivityId = line.ActivityId;
        ActivityType = line.ActivityType;
        ActivityDate = line.ActivityDate;
        DocumentCode = line.DocumentCode;
        Description = line.Description;
        ExpectedIncome = line.ExpectedIncome;
        Expenses = line.Expenses;
    }
}
