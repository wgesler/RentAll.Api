namespace RentAll.Domain.Models;

public class OwnerCashReport
{
    public List<OwnerCashReportRow> Rows { get; set; } = [];
    public List<OwnerStatementPropertyActivityLine> PropertyActivityLines { get; set; } = [];
}
