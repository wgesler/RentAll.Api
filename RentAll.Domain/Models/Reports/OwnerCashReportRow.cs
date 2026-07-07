namespace RentAll.Domain.Models;

public class OwnerCashReportRow
{
    public Guid PropertyId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public Guid? OwnerId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public decimal StartingBalance { get; set; }
    public decimal ReceivedIncome { get; set; }
    public decimal OwnerExpenses { get; set; }
    public decimal OwnerPayment { get; set; }
    public decimal EndingBalance { get; set; }
    public decimal WorkingCapital { get; set; }
}
