namespace RentAll.Api.Dtos.Leads.Rentals;

public class CreateExternalLeadRentalDto : CreateLeadRentalDto
{
    public (bool IsValid, string? ErrorMessage) IsValid() => base.IsValid(currentOffices: null);
}
