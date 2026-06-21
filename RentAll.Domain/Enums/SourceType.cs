namespace RentAll.Domain.Enums;

public enum SourceType
{
    Check = 0,
    Deposit = 1,
    Invoice = 2,
    InvoicePayment = 3,
    InvoiceCredit = 4,
    Bill = 5,
    BillPayment = 6,
    BillCredit = 7,
    Receipt = 8,
    CreditMemo = 9,
    Journal = 10,
    Adjustment = 11,
    CreditCard = 12,
    CreditCardCredit = 13,
    CreditCardRefund = 14,
    SecurityDeposit = 15,
    OwnerDistribution = 16,
    Paycheck = 17,
    PayrollLiabilityCheck = 18,
    YtdAdjustment = 19,
    LiabilityAdjustment = 20,
    Transfer = 21,
    WorkOrder = 22
}

public static class SourceTypeExtensions
{
    private static readonly Dictionary<SourceType, string> SourceTypeCodes = new()
    {
        { SourceType.Check, "CK" },
        { SourceType.Deposit, "DEP" },
        { SourceType.Invoice, "INV" },
        { SourceType.InvoicePayment, "PAY" },
        { SourceType.InvoiceCredit, "PAY" },
        { SourceType.Bill, "BILL" },
        { SourceType.BillPayment, "BPAY" },
        { SourceType.BillCredit, "BCRD" },
        { SourceType.Receipt, "REC" },
        { SourceType.CreditMemo, "CMEM" },
        { SourceType.Journal, "JRN" },
        { SourceType.Adjustment, "ADJ" },
        { SourceType.CreditCard, "CC" },
        { SourceType.CreditCardCredit, "CCC" },
        { SourceType.CreditCardRefund, "CCRF" },
        { SourceType.SecurityDeposit, "SDEP" },
        { SourceType.OwnerDistribution, "ODIS" },
        { SourceType.Paycheck, "PAY" },
        { SourceType.PayrollLiabilityCheck, "PLB" },
        { SourceType.YtdAdjustment, "YADJ" },
        { SourceType.LiabilityAdjustment, "LADJ" },
        { SourceType.Transfer, "TRAN" },
        { SourceType.WorkOrder, "WO"},
    };

    private static readonly Dictionary<string, SourceType> CodeToSourceType = new()
    {
        { "CK", SourceType.Check },
        { "DEP", SourceType.Deposit },
        { "INV", SourceType.Invoice },
        { "BILL", SourceType.Bill },
        { "BPAY", SourceType.BillPayment },
        { "BCRD", SourceType.BillCredit },
        { "REC", SourceType.Receipt },
        { "CMEM", SourceType.CreditMemo },
        { "JRN", SourceType.Journal },
        { "ADJ", SourceType.Adjustment },
        { "CC", SourceType.CreditCard },
        { "CCC", SourceType.CreditCardCredit },
        { "CCRF", SourceType.CreditCardRefund },
        { "SDEP", SourceType.SecurityDeposit },
        { "ODIS", SourceType.OwnerDistribution },
        { "PLB", SourceType.PayrollLiabilityCheck },
        { "YADJ", SourceType.YtdAdjustment },
        { "LADJ", SourceType.LiabilityAdjustment },
        { "TRAN", SourceType.Transfer },
        { "WO", SourceType.WorkOrder},
    };

    public static string ToCode(this SourceType sourceType)
    {
        return SourceTypeCodes.TryGetValue(sourceType, out var code) ? code : string.Empty;
    }

    public static SourceType? FromCode(string code)
    {
        return CodeToSourceType.TryGetValue(code?.ToUpper() ?? string.Empty, out var sourceType)
            ? sourceType
            : null;
    }
}
