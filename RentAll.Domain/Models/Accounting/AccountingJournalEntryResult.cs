namespace RentAll.Domain.Models;

public sealed class AccountingJournalEntryResult
{
    public JournalEntry? JournalEntry { get; init; }
    public string? Warning { get; init; }

    public bool HasWarning => !string.IsNullOrWhiteSpace(Warning);

    public static AccountingJournalEntryResult Success(JournalEntry? journalEntry = null)
        => new() { JournalEntry = journalEntry };

    public static AccountingJournalEntryResult WarningResult(string warning, JournalEntry? journalEntry = null)
        => new() { JournalEntry = journalEntry, Warning = warning };
}
