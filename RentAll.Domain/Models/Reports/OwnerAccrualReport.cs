namespace RentAll.Domain.Models;

public class OwnerAccrualReport
{
    public List<OwnerAccrualReportRow> Rows { get; set; } = [];
    public List<OwnerStatementPropertyActivityLine> PropertyActivityLines { get; set; } = [];
}
