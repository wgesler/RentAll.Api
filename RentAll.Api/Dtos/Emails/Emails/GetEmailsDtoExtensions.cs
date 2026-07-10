namespace RentAll.Api.Dtos.Emails.Emails;

public static class GetEmailsDtoExtensions
{
    public static EmailGetCriteria ToCriteria(this GetEmailsDto dto, Guid organizationId)
    {
        return new EmailGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = dto.ResolvedOfficeIds,
            PropertyId = dto.PropertyId,
            ReservationId = dto.ReservationId,
            EmailTypeIds = string.IsNullOrWhiteSpace(dto.EmailTypeIds) ? null : dto.EmailTypeIds.Trim(),
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        };
    }
}
