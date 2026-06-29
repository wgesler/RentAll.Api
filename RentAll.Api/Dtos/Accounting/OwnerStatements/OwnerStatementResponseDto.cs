using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Accounting.OwnerStatements;

public class OwnerStatementResponseDto
{
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal Balance { get; set; }

    public OwnerStatementResponseDto(OwnerStatementSummary statement)
    {
        PropertyId = statement.PropertyId;
        PropertyCode = statement.PropertyCode;
        OwnerName = statement.OwnerName;
        Income = statement.Income;
        Expenses = statement.Expenses;
        Balance = statement.Balance;
    }
}
