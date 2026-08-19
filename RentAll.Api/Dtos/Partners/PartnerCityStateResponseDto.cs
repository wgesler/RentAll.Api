namespace RentAll.Api.Dtos.Partners;

public class PartnerCityStateResponseDto
{
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;

    public PartnerCityStateResponseDto(Domain.Models.Partners.PartnerCityState cityState)
    {
        City = cityState.City;
        State = cityState.State;
    }
}
