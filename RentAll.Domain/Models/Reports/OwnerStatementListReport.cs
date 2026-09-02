namespace RentAll.Domain.Models;

public class OwnerStatementListReport
{
    public List<OwnerCashReportRow> Rows { get; set; } = [];
    public List<OwnerInvoiceOutstanding> OutstandingInvoices { get; set; } = [];
}
