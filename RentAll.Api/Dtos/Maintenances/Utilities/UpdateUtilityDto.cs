namespace RentAll.Api.Dtos.Maintenances.Utilities;

public class UpdateUtilityDto
{
    public int UtilityId { get; set; }
    public Guid PropertyId { get; set; }
    public string UtilityName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (UtilityId <= 0)
            return (false, "UtilityId is required");

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
            UtilityId = UtilityId,
            PropertyId = PropertyId,
            UtilityName = UtilityName,
            Phone = Phone,
            AccountName = AccountName,
            AccountNumber = AccountNumber,
            Notes = Notes
        };
    }
}
