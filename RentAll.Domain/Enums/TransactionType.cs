namespace RentAll.Domain.Enums;

public enum TransactionType
{
	Debit = 0,
	Charge = 1,
	SecurityDeposit = 2,
	SecurityDepositWaiver = 3,

	Credit = 10,
	Payment = 11,
	Refund = 12,
	Revenue = 13,
	Adjustment = 14
}
