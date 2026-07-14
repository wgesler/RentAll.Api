namespace RentAll.Api.Dtos.Organizations.Accounting;

public class UpdateAccountingOfficeCheckNumberDto
{
    public int CurrentCheckNumber { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (CurrentCheckNumber < 1)
            return (false, "CurrentCheckNumber must be at least 1");

        return (true, null);
    }
}

public class AccountingOfficeCheckNumberResponseDto
{
    public int OfficeId { get; set; }
    public int CurrentCheckNumber { get; set; }

    public AccountingOfficeCheckNumberResponseDto(int officeId, int currentCheckNumber)
    {
        OfficeId = officeId;
        CurrentCheckNumber = currentCheckNumber;
    }
}
