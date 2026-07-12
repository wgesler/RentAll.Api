namespace RentAll.Domain.Models;

public enum JournalEntryMemoCategory
{
    None = 0,
    OwnerRent,
    OwnerRentActual,
    OwnerPayment,
    OwnerStartingBalance,
    OwnerBill,
    OwnerWorkOrder,
    OwnerLinenAndTowel,
    Payment,
    PrePayment,
    Transfer,
    Deposit,
    Invoice,
    AccountsReceivable
}

public sealed class JournalEntryMemoMatch
{
    public static JournalEntryMemoMatch None { get; } = new();

    public JournalEntryMemoCategory Category { get; init; }
    public string SourceCode { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;

    public bool IsMatch => Category != JournalEntryMemoCategory.None;
}
