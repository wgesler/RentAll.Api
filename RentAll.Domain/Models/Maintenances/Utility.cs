namespace RentAll.Domain.Models;

public class Utility
{
    public int UtilityId { get; set; }
    public Guid PropertyId { get; set; }
    public string UtilityName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
