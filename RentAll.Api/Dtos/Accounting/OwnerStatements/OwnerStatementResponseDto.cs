using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Accounting.OwnerStatements;

public class OwnerStatementResponseDto
{
    public Guid PropertyId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public Guid? OwnerId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public decimal Expected { get; set; }
    public decimal PrePaid { get; set; }
    public decimal Outstanding { get; set; }
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal Balance { get; set; }
    public decimal WorkingCapital { get; set; }
    public decimal WorkingCapitalBalanceDue { get; set; }

    public OwnerStatementResponseDto(OwnerStatementSummary statement)
    {
        PropertyId = statement.PropertyId;
        OfficeId = statement.OfficeId;
        OfficeName = statement.OfficeName;
        OwnerId = statement.OwnerId;
        PropertyCode = statement.PropertyCode;
        OwnerName = statement.OwnerName;
        Expected = statement.Expected;
        PrePaid = statement.PrePaid;
        Outstanding = statement.Outstanding;
        Income = statement.Income;
        Expenses = statement.Expenses;
        Balance = statement.Balance;
        WorkingCapital = statement.WorkingCapital;
        WorkingCapitalBalanceDue = statement.WorkingCapitalBalanceDue;
    }
}
