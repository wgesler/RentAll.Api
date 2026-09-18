namespace RentAll.Infrastructure.Entities.Properties;

public class PropertyICalEntity
{
    public int PropertyICalId { get; set; }
    public Guid PropertyId { get; set; }
    public string ICalUrl { get; set; } = string.Empty;
}
