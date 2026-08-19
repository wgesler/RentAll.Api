namespace RentAll.Api.Dtos.Partners;

public class PartnerContactResponseDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public PartnerContactResponseDto(Domain.Models.Partners.PartnerContact contact)
    {
        CompanyName = contact.CompanyName;
        Name = contact.Name;
        Phone = contact.Phone;
        Email = contact.Email;
    }
}
