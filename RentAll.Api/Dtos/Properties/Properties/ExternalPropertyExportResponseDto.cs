namespace RentAll.Api.Dtos.Properties.Properties;

public class ExternalPropertyExportResponseDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public List<ExternalPropertyExportItemDto> Properties { get; set; } = [];
}
