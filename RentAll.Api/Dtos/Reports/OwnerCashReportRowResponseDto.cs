using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Reports;

public class OwnerCashReportRowResponseDto
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

    public OwnerCashReportRowResponseDto(OwnerCashReportRow row)
    {
        PropertyId = row.PropertyId;
        OfficeId = row.OfficeId;
        OfficeName = row.OfficeName;
        OwnerId = row.OwnerId;
        PropertyCode = row.PropertyCode;
        OwnerName = row.OwnerName;
        StartingBalance = row.StartingBalance;
        ReceivedIncome = row.ReceivedIncome;
        OwnerExpenses = row.OwnerExpenses;
        OwnerPayment = row.OwnerPayment;
        EndingBalance = row.EndingBalance;
        WorkingCapital = row.WorkingCapital;
    }
}
