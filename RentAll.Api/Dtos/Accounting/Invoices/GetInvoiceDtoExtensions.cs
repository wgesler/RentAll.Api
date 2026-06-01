using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Accounting.Invoices;

public static class GetInvoiceDtoExtensions
{
    public static InvoiceGetCriteria ToCriteria(this GetInvoiceDto dto, Guid organizationId)
    {
        return new InvoiceGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = dto.ResolvedOfficeIds,
            ReservationId = dto.ReservationId,
            PropertyId = dto.PropertyId,
            InvoiceCode = string.IsNullOrWhiteSpace(dto.InvoiceCode) ? null : dto.InvoiceCode.Trim(),
            IncludeInactive = dto.IncludeInactive,
            IncludePaid = dto.IncludePaid,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        };
    }
}
