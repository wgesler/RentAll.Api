namespace RentAll.Api.Dtos.Leads.Owners;

public class CreateExternalLeadOwnerDto : CreateLeadOwnerDto
{
    public (bool IsValid, string? ErrorMessage) IsValid() => base.IsValid(currentOffices: null);
}
