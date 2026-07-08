using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Reports;

public class OwnerCashReportRowResponseDto
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
    public decimal ReceivedIncome { get; set; }
    public decimal OwnerExpenses { get; set; }
    public decimal OwnerPayment { get; set; }
    public decimal EndingBalance { get; set; }
    public decimal WorkingCapital { get; set; }

    public OwnerCashReportRowResponseDto(OwnerCashReportRow row)
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
        ReceivedIncome = row.ReceivedIncome;
        OwnerExpenses = row.OwnerExpenses;
        OwnerPayment = row.OwnerPayment;
        EndingBalance = row.EndingBalance;
        WorkingCapital = row.WorkingCapital;
    }
}
