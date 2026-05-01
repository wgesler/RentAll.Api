namespace RentAll.Infrastructure.Entities.Properties;

public class PropertyPhotoEntity
{
    public int PhotoId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid OrganizationId { get; set; }
    public int Order { get; set; }
    public string PhotoPath { get; set; } = string.Empty;
}
