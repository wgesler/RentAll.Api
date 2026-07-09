namespace RentAll.Domain.Models;

internal sealed class OwnerStartingBalance
{
    public int OfficeId { get; set; }
    public Guid PropertyId { get; set; }
    public decimal LedgerBalance { get; set; }
    public decimal OpeningAccountsPayableAmount { get; set; }
    public DateOnly? OpeningBalanceTransactionDate { get; set; }
}
