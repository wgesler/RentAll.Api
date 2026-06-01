using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Emails.Alerts;

public static class GetAlertDtoExtensions
{
    public static AlertGetCriteria ToCriteria(this GetAlertDto dto, Guid organizationId)
    {
        return new AlertGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = dto.ResolvedOfficeIds,
            PropertyId = dto.PropertyId,
            ReservationId = dto.ReservationId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        };
    }
}
