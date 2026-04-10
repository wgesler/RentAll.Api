namespace RentAll.Api.Dtos.Maintenances.Utilities;

public class UtilityResponseDto
{
    public int UtilityId { get; set; }
    public Guid PropertyId { get; set; }
    public string UtilityName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AccountName { get; set; }
    public string? AccountNumber { get; set; }
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
