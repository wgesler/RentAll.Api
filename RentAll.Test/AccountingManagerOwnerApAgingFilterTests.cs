using RentAll.Domain.Enums;
using RentAll.Domain.Managers;
using RentAll.Domain.Models;

namespace RentAll.Test;

public class AccountingManagerOwnerApAgingFilterTests
{
    [Fact]
    public void ApplyOwnerApOpeningBalanceCutoffFilter_KeepsOpeningBalanceAndLaterOnly()
    {
        var propertyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        const int officeId = 5;
        var cutoff = new DateOnly(2026, 5, 31);
        var cutoffs = new Dictionary<string, DateOnly>(StringComparer.OrdinalIgnoreCase)
        {
            [$"{officeId}|{propertyId}"] = cutoff
        };

        var lines = new List<JournalEntryLineSearchResult>
        {
            new()
            {
                JournalEntryLineId = Guid.NewGuid(),
                PropertyId = propertyId,
                OfficeId = officeId,
                TransactionDate = new DateOnly(2026, 1, 1),
                JournalEntryCode = "R-000134-001"
            },
            new()
            {
                JournalEntryLineId = Guid.NewGuid(),
                PropertyId = propertyId,
                OfficeId = officeId,
                TransactionDate = cutoff,
                JournalEntryCode = "JE-011769",
                JournalEntryKindId = (int)JournalEntryKind.OwnerStartingBalance,
                JournalEntryMemo = "ZU-BWF13: Owner: BAL-05-2026"
            },
            new()
            {
                JournalEntryLineId = Guid.NewGuid(),
                PropertyId = propertyId,
                OfficeId = officeId,
                TransactionDate = new DateOnly(2026, 6, 1),
                JournalEntryCode = "R-000134-006"
            }
        };

        var filtered = AccountingManager.ApplyOwnerApOpeningBalanceCutoffFilter(lines, cutoffs);

        Assert.Equal(2, filtered.Count);
        Assert.DoesNotContain(filtered, line => line.JournalEntryCode == "R-000134-001");
        Assert.Contains(filtered, line => line.JournalEntryCode == "JE-011769");
        Assert.Contains(filtered, line => line.JournalEntryCode == "R-000134-006");
    }
}
