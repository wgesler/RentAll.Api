using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Reports;

public class OwnerInvoiceOutstandingResponseDto
{
    public Guid PropertyId { get; set; }
    public int OfficeId { get; set; }
    public Guid InvoiceId { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public DateOnly AccountingPeriod { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal ExpectedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal Outstanding { get; set; }

    public OwnerInvoiceOutstandingResponseDto(OwnerInvoiceOutstanding row)
    {
        PropertyId = row.PropertyId;
        OfficeId = row.OfficeId;
        InvoiceId = row.InvoiceId;
        InvoiceCode = row.InvoiceCode;
        AccountingPeriod = row.AccountingPeriod;
        Description = row.Description;
        ExpectedAmount = row.ExpectedAmount;
        ActualAmount = row.ActualAmount;
        Outstanding = row.Outstanding;
    }
}
