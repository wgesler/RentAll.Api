using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Reports;

public class OwnerAccrualReportRowResponseDto
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
    public decimal PrepaidIncome { get; set; }
    public decimal PaidIncome { get; set; }
    public decimal UnpaidIncome { get; set; }
    public decimal OwnerExpenses { get; set; }
    public decimal OwnerProfit { get; set; }

    public OwnerAccrualReportRowResponseDto(OwnerAccrualReportRow row)
    {
        PropertyId = row.PropertyId;
        OfficeId = row.OfficeId;
        OfficeName = row.OfficeName;
        OwnerId = row.OwnerId;
        PropertyCode = row.PropertyCode;
        CompanyName = row.CompanyName;
        OwnerNames = row.OwnerNames;
        OwnerNameLine = row.OwnerNameLine;
        StartingBalance = row.StartingBalance;
        InvoicedIncome = row.InvoicedIncome;
        PrepaidIncome = row.PrepaidIncome;
        PaidIncome = row.PaidIncome;
        UnpaidIncome = row.UnpaidIncome;
        OwnerExpenses = row.OwnerExpenses;
        OwnerProfit = row.OwnerProfit;
    }
}
