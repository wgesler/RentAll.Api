namespace RentAll.Api.Dtos.Accounting.OwnerStatements;

public class CreateOwnerStatementStartingBalanceDto
{
    public int OfficeId { get; set; }
    public Guid OwnerId { get; set; }
    public Guid PropertyId { get; set; }
    public DateOnly TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public string CurrentPassword { get; set; } = string.Empty;

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OfficeId <= 0)
            return (false, "OfficeId is required");
        if (OwnerId == Guid.Empty)
            return (false, "OwnerId is required");
        if (PropertyId == Guid.Empty)
            return (false, "PropertyId is required");
        if (TransactionDate == default)
            return (false, "TransactionDate is required");
        if (Amount == 0)
            return (false, "Amount must be non-zero");
        return (true, null);
    }
}
