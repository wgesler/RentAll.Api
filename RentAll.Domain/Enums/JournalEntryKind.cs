namespace RentAll.Domain.Enums;

public enum JournalEntryKind
{
    Manual = 0,
    Charge = 1,
    OwnerExpected = 2,
    Payment = 3,
    PrePaymentReceive = 4,
    PrePaymentApply = 5,
    OwnerActual = 6,
    Bill = 10,
    BillPayment = 11,
    Receipt = 12,
    OwnerUtility = 13,
    CrossOfficeCreditCard = 14,
    Expense = 20,
    OwnerTransfer = 21,
    Deposit = 30,
    Transfer = 40,
    DepartureFee = 50,
    SecurityDepositReturn = 51,
    SecurityDepositTransfer = 52,
    LinenTowelFee = 60,
    LinenTowelUnusedReversal = 61,
    RetainedEarnings = 100,
    OwnerStartingBalance = 110
}
