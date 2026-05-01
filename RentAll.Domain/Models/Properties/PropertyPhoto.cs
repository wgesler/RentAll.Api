namespace RentAll.Domain.Models.Properties;

public class PropertyPhoto
{
    public int PhotoId { get; set; }
    public Guid PropertyId { get; set; }
    public int Order { get; set; }
    public string PhotoPath { get; set; } = string.Empty;
}
