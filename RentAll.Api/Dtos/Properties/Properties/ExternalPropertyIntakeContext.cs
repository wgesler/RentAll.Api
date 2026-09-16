namespace RentAll.Api.Dtos.Properties.Properties;

public sealed class ExternalPropertyIntakeContext
{
    public Guid OrganizationId { get; init; }
    public int OfficeId { get; init; }
    public Guid PartnerVendorId { get; init; }
}
