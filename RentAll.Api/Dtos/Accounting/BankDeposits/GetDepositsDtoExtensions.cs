namespace RentAll.Api.Dtos.Accounting.BankDeposits;

public static class GetDepositsDtoExtensions
{
    public static DepositGetCriteria ToCriteria(this GetDepositsDto dto, Guid organizationId)
    {
        return new DepositGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = dto.ResolvedOfficeIds,
            PropertyId = dto.PropertyId,
            IsActive = dto.IsActive,
            IncludeInactive = dto.IncludeInactive,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        };
    }
}
