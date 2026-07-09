namespace RentAll.Domain.Models;

internal sealed class RecapLineSet
{
    public List<JournalEntryRecapLine> AllLines { get; init; } = [];
    public List<JournalEntryRecapLine> ActivityLines { get; init; } = [];
}
