using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Reports;

public class EscrowOfficeBalanceResponseDto
{
    public int OfficeId { get; set; }
    public int AccountId { get; set; }
    public string AccountNo { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Balance { get; set; }

    public EscrowOfficeBalanceResponseDto(EscrowOfficeBalance balance)
    {
        OfficeId = balance.OfficeId;
        AccountId = balance.AccountId;
        AccountNo = balance.AccountNo;
        AccountName = balance.AccountName;
        Balance = balance.Balance;
    }
}
