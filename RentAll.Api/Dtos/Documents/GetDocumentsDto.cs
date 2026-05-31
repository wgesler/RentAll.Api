namespace RentAll.Api.Dtos.Documents;

public class GetDocumentsDto
{
    public int[] OfficeIds { get; set; } = [];
    public Guid? PropertyId { get; set; }
    public string? DocumentTypeIds { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public string ResolvedOfficeIds => string.Join(",", OfficeIds);

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OfficeIds == null || OfficeIds.Length == 0)
            return (false, "At least one office is required");

        if (OfficeIds.Any(id => id <= 0))
            return (false, "Each office ID must be a positive integer");

        if (!string.IsNullOrWhiteSpace(DocumentTypeIds))
        {
            var typeIds = DocumentTypeIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (typeIds.Length == 0)
                return (false, "DocumentTypeIds must contain at least one ID when provided");

            foreach (var typeId in typeIds)
            {
                if (!int.TryParse(typeId, out var parsed) || parsed < 0)
                    return (false, $"Invalid document type ID: {typeId}");
            }
        }

        if (StartDate.HasValue && EndDate.HasValue && EndDate.Value < StartDate.Value)
            return (false, "EndDate must be on or after StartDate");

        return (true, null);
    }
}
