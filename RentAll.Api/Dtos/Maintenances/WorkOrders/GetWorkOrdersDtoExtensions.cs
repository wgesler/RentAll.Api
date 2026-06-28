using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Maintenances.WorkOrders;

public static class GetWorkOrdersDtoExtensions
{
    public static WorkOrderGetCriteria ToCriteria(this GetWorkOrdersDto dto, Guid organizationId)
    {
        return new WorkOrderGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = dto.ResolvedOfficeIds,
            PropertyId = dto.PropertyId,
            IsActive = dto.IsActive,
            IncludeInactive = dto.IncludeInactive,
            InactiveOnly = dto.InactiveOnly,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        };
    }
}
