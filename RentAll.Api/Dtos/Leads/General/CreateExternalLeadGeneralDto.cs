namespace RentAll.Api.Dtos.Leads.General;

public class CreateExternalLeadGeneralDto : CreateLeadGeneralDto
{
    public (bool IsValid, string? ErrorMessage) IsValid() => base.IsValid(currentOffices: null);
}
