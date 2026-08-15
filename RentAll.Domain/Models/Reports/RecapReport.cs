namespace RentAll.Domain.Models;

public class RecapReport
{
    public List<RecapReportRow> Rows { get; set; } = [];
    public string RentalIncomeParentAccountNo { get; set; } = string.Empty;
}
