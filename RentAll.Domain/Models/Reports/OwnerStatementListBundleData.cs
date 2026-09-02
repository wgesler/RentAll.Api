namespace RentAll.Domain.Models;

public class OwnerStatementListBundleData
{
    public List<OwnerCashReportRow> Rows { get; init; } = [];
    public List<OwnerInvoiceOutstanding> OutstandingInvoices { get; init; } = [];
}
