namespace RentAll.Api.Dtos.Documents;

public static class GetDocumentsDtoExtensions
{
    public static DocumentGetCriteria ToCriteria(this GetDocumentsDto dto, Guid organizationId)
    {
        return new DocumentGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = dto.ResolvedOfficeIds,
            PropertyId = dto.PropertyId,
            DocumentTypeIds = string.IsNullOrWhiteSpace(dto.DocumentTypeIds) ? null : dto.DocumentTypeIds.Trim(),
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        };
    }
}
