namespace RentAll.Api.Dtos.Accounting.JournalEntryLines;

public static class GetJournalEntryLineDtoExtensions
{
    public static JournalEntryLineGetCriteria ToCriteria(this GetJournalEntryLineDto dto, Guid organizationId)
    {
        return new JournalEntryLineGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = dto.ResolvedOfficeIds,
            ChartOfAccountId = dto.ResolvedChartOfAccountId,
            SourceTypeId = dto.SourceTypeId,
            SourceId = dto.SourceId,
            ReservationId = dto.ReservationId,
            PropertyId = dto.PropertyId,
            ContactId = dto.ContactId,
            IncludeVoided = dto.IncludeVoided,
            IncludeUnposted = dto.IncludeUnposted,
            UnclearedOnly = dto.UnclearedOnly,
            IncludeCashOnly = dto.IncludeCashOnly,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        };
    }
}
