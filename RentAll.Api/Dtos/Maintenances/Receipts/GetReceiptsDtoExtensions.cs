using RentAll.Domain.Constants;

namespace RentAll.Api.Dtos.Maintenances.Receipts;

public static class GetReceiptsDtoExtensions
{
    public static ReceiptGetCriteria ToCriteria(this GetReceiptsDto dto, Guid organizationId)
    {
        var propertyId = dto.PropertyId;
        if (propertyId.HasValue && ReceiptPropertyConstants.IsCompanyPropertyId(propertyId.Value))
            propertyId = null;

        return new ReceiptGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = dto.ResolvedOfficeIds,
            PropertyId = propertyId,
            IsActive = dto.IsActive,
            IncludeInactive = dto.IncludeInactive,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            ReceiptKind = dto.ReceiptKind,
            VendorId = dto.VendorId
        };
    }
}
