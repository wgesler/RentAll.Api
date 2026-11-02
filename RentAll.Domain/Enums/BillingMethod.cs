namespace RentAll.Domain.Enums;

public enum BillingMethod
{
    Invoice = 0,
    AchDebit = 1,
    Realtime = 2,
    CreditCardVisa = 3,
    CreditCardMc = 4,
    CreditCardAmex = 5,
    CreditCardDiscover = 6
}

public static class BillingMethodHelper
{
    public static bool IsCreditCard(BillingMethod billingMethod)
    {
        return billingMethod is BillingMethod.CreditCardVisa
            or BillingMethod.CreditCardMc
            or BillingMethod.CreditCardAmex
            or BillingMethod.CreditCardDiscover;
    }
}
