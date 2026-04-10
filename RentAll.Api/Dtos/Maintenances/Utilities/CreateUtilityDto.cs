namespace RentAll.Api.Dtos.Maintenances.Utilities;

public class CreateUtilityDto
{
    public Guid PropertyId { get; set; }
    public string UtilityName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AccountName { get; set; }
    public string? AccountNumber { get; set; }
    public string? Notes { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (PropertyId == Guid.Empty)
            return (false, "PropertyId is required");

        if (string.IsNullOrWhiteSpace(UtilityName))
            return (false, "UtilityName is required");

        return (true, null);
    }

    public Utility ToModel()
    {
        return new Utility
        {
            PropertyId = PropertyId,
            UtilityName = UtilityName,
            Phone = Phone,
            AccountName = AccountName,
            AccountNumber = AccountNumber,
            Notes = Notes
        };
    }
}
