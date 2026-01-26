namespace RentAll.Domain.Enums;

public enum TransactionType
{
	Debit = 0,
	Credit = 1,
	Payment = 2,
	Refund = 3,
	Charge = 4,
	SecurityDeposit = 5,
	SecurityDepositWaiver = 6,
	Adjustment = 7
}
