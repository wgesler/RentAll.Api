using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Accounting.JournalEntries;

public static class GetJournalEntryDtoExtensions
{
    public static JournalEntryGetCriteria ToCriteria(this GetJournalEntryDto dto, Guid organizationId)
    {
        return new JournalEntryGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = dto.ResolvedOfficeIds,
            SourceTypeId = dto.SourceTypeId,
            SourceId = dto.SourceId,
            TransactionTypeId = dto.TransactionTypeId,
            IncludeVoided = dto.IncludeVoided,
            IncludeUnposted = dto.IncludeUnposted,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        };
    }
}
