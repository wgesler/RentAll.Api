namespace RentAll.Api.Dtos.Accounting.Transfers;

public static class GetTransfersDtoExtensions
{
    public static TransferGetCriteria ToCriteria(this GetTransfersDto dto, Guid organizationId)
    {
        return new TransferGetCriteria
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
