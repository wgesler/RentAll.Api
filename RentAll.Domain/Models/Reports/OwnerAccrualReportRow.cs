namespace RentAll.Domain.Models;

public class OwnerAccrualReportRow
{
    public Guid PropertyId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public Guid? OwnerId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string OwnerNames { get; set; } = string.Empty;
    public string OwnerNameLine { get; set; } = string.Empty;
    public decimal StartingBalance { get; set; }
    public decimal InvoicedIncome { get; set; }
    public decimal UnpaidIncome { get; set; }
    public decimal OwnerExpenses { get; set; }
    public decimal OwnerProfit { get; set; }
}
