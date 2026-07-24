namespace RentAll.Api.Dtos.Accounting.OwnerStatements;

public class GetOwnerStatementStartingBalanceDto
{
    public int OfficeId { get; set; }
    public Guid OwnerId { get; set; }
    public Guid PropertyId { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OfficeId <= 0)
            return (false, "OfficeId is required");
        if (PropertyId == Guid.Empty)
            return (false, "PropertyId is required");

        return (true, null);
    }
}
