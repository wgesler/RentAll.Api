namespace RentAll.Domain.Models;

public class Utility
{
    public int UtilityId { get; set; }
    public Guid PropertyId { get; set; }
    public string UtilityName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AccountName { get; set; }
    public string? AccountNumber { get; set; }
    public string? Notes { get; set; }
}
