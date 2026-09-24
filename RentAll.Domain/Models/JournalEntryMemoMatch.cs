namespace RentAll.Domain.Models;

public enum JournalEntryMemoCategory
{
    None = 0,
    OwnerRent,
    OwnerRentActual,
    OwnerPayment,
    OwnerStartingBalance,
    OfficeOpeningBalanceSheet,
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
    /// <summary>One or more invoice codes before <c>: Payment:</c> (consolidated payments list each code).</summary>
    public IReadOnlyList<string> SourceCodes { get; init; } = Array.Empty<string>();
    public string Detail { get; init; } = string.Empty;

    public bool IsMatch => Category != JournalEntryMemoCategory.None;
}
