using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Maintenances.Receipts;

public static class GetReceiptsDtoExtensions
{
    public static ReceiptGetCriteria ToCriteria(this GetReceiptsDto dto, Guid organizationId)
    {
        return new ReceiptGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = dto.ResolvedOfficeIds,
            PropertyId = dto.PropertyId,
            IncludeInactive = dto.IncludeInactive,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        };
    }
}
