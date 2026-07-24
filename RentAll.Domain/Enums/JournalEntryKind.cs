namespace RentAll.Domain.Enums;

public enum JournalEntryKind
{
    Manual = 0,
    OpeningBalanceSheet = 1,
    RetainedEarnings = 2,
    Charge = 11,
    OwnerExpected = 12,
    Payment = 13,
    PrePaymentReceive = 14,
    PrePaymentApply = 15,
    OwnerActual = 16,
    Bill = 20,
    BillPayment = 21,
    Receipt = 22,
    OwnerUtility = 23,
    CrossOfficeCreditCard = 24,
    Deposit = 30,
    Transfer = 40,
    Expense = 50,
    OwnerTransfer = 51,
    DepartureFee = 60,
    SecurityDepositReturn = 61,
    SecurityDepositTransfer = 62,
    LinenTowelFee = 70,
    LinenTowelUnusedReversal = 71
}
