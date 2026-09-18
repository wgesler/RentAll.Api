namespace RentAll.Domain.Models.Properties;

public class PropertyICal
{
    public int PropertyICalId { get; set; }
    public Guid PropertyId { get; set; }
    public string ICalUrl { get; set; } = string.Empty;
}
