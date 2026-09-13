namespace RentAll.Api.Dtos.Maintenances.ReceiptDrafts;

public static class GetReceiptDraftsDtoExtensions
{
    public static ReceiptDraftGetCriteria ToCriteria(this GetReceiptDraftsDto dto, Guid organizationId)
    {
        return new ReceiptDraftGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = dto.ResolvedOfficeIds,
            PropertyId = dto.PropertyId,
            IsActive = dto.IsActive,
            IncludeInactive = dto.IncludeInactive,
            IncludePromoted = dto.IncludePromoted,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            ReceiptKind = dto.ReceiptKind,
            VendorId = dto.VendorId,
            DraftSourceFlags = dto.DraftSourceFlags
        };
    }
}
