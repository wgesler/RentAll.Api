namespace RentAll.Api.Dtos.Maintenances.Utilities;

public class UtilityResponseDto
{
    public int UtilityId { get; set; }
    public Guid PropertyId { get; set; }
    public string UtilityName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public UtilityResponseDto(Utility utility)
    {
        UtilityId = utility.UtilityId;
        PropertyId = utility.PropertyId;
        UtilityName = utility.UtilityName;
        Phone = utility.Phone;
        AccountName = utility.AccountName;
        AccountNumber = utility.AccountNumber;
        Notes = utility.Notes;
    }
}
