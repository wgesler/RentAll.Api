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
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        };
    }
}
